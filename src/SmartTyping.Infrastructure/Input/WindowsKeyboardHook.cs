using System.Text;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Application.Settings;
using SmartTyping.Domain.Enums;
using SmartTyping.Domain.ValueObjects;

namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Low-level keyboard hook (<c>WH_KEYBOARD_LL</c>) that raises <see cref="ConversionHotkeyPressed"/>
/// when Ctrl+Shift+L is pressed and <see cref="ExpansionHotkeyPressed"/> when Ctrl+Shift+E is
/// pressed. Passive: it observes input and never rewrites keystrokes (no automatic correction).
/// Any exception in the hook callback is logged and swallowed so the host application can never be
/// crashed by the hook.
///
/// <para><b>Threading:</b> <see cref="Start"/> must be called on a thread with a running message
/// loop (the WPF UI thread). The event is raised on that thread.</para>
/// </summary>
public sealed class WindowsKeyboardHook : IKeyboardHook
{
    private readonly ILogger<WindowsKeyboardHook> _logger;
    private readonly IKeyboardLayoutConverter _converter;
    private readonly LayoutDecider _decider;
    private readonly IForegroundApp _foregroundApp;
    private readonly ICaretContext _caret;

    // Keep the delegates alive for the lifetime of the hooks (prevents GC of the callbacks).
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly NativeMethods.LowLevelKeyboardProc _mouseProc;
    private readonly NativeMethods.WinEventProc _focusProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private IntPtr _mouseHookHandle = IntPtr.Zero;
    private IntPtr _focusHookHandle = IntPtr.Zero;
    private IntPtr _foregroundHookHandle = IntPtr.Zero;

    // Current action→hotkey bindings (defaults until UpdateBindings is called). Volatile swap.
    private IReadOnlyDictionary<HotkeyAction, Hotkey> _bindings = SettingsService.DefaultHotkeys;

    // As-you-type suggestion state (accessed only on the hook/UI thread — both low-level hooks are
    // called on the thread that installed them, so no locking is needed).
    // Long enough for any word in either dictionary, with room to spare. A run that outgrows it is
    // not a word we could act on anyway.
    private const int MaxTrackedWordLength = 32;

    private readonly StringBuilder _wordBuffer = new(MaxTrackedWordLength);

    // What the current run has actually put on screen. Under a latin layout this is the same as
    // _wordBuffer; under the Thai layout the keys produce Thai, and a keystroke that would produce an
    // impossible sequence is swallowed (see ThaiInput) so that it appears in no application at all.
    // This — not a prediction — is what a correction deletes.
    private readonly StringBuilder _onScreen = new(MaxTrackedWordLength);

    // Whether we saw the character that precedes the run: true when the run began at a delimiter we
    // typed, which hosts no Thai mark. After a mouse click, a navigation key or a window switch the
    // caret sits after a character we never watched, and _precedingProbed answers for it instead.
    private bool _precedingKnown;

    // What UI Automation says sits before the caret: -1 while unknown, otherwise the character (0 when
    // the caret is at the very start). Written by the probe on a thread-pool thread, read by the hook.
    private const int PrecedingUnknown = -1;
    private volatile int _precedingProbed = PrecedingUnknown;

    // How long the hook may wait for an in-flight probe. Windows drops a low-level hook that takes
    // longer than LowLevelHooksTimeout (300 ms by default) to return, so this stays far below it.
    private const int PrecedingWaitMs = 60;
    private const int PrecedingPollMs = 5;

    // Invalidates a probe whose answer arrived after the caret moved again.
    private int _precedingGeneration;

    // Set when a run outgrows the buffer. We can no longer say how many characters sit behind the
    // caret, so acting on the tail would replace text at the wrong place. Tracking stays off until
    // the next real word break.
    private bool _wordAbandoned;

    private string _lastSuggestedWord = string.Empty;
    private IntPtr _lastForeground = IntPtr.Zero;

    // True from the moment a correction is applied until the user types anything else. Guards the
    // undo hotkey so it only fires when the correction really is the last thing that happened.
    private volatile bool _undoArmed;

    public WindowsKeyboardHook(
        ILogger<WindowsKeyboardHook> logger,
        IKeyboardLayoutConverter converter,
        LayoutDecider decider,
        IForegroundApp foregroundApp,
        ICaretContext caret)
    {
        _logger = logger;
        _converter = converter;
        _decider = decider;
        _foregroundApp = foregroundApp;
        _caret = caret;
        _proc = HookCallback;
        _mouseProc = MouseCallback;
        _focusProc = FocusCallback;
    }

    public event EventHandler? ConversionHotkeyPressed;

    public event EventHandler? ExpansionHotkeyPressed;

    public event EventHandler? PickerHotkeyPressed;

    public event EventHandler? CaptureHotkeyPressed;

    public event EventHandler? AiImproveHotkeyPressed;

    public event EventHandler<LayoutSuggestion>? LayoutSuggestionRaised;

    public event EventHandler<LayoutCorrection>? LayoutAutoCorrectRequested;

    public event EventHandler<WordBoundary>? SnippetWordCompleted;

    public event EventHandler? UndoCorrectionRequested;

    public event EventHandler<WordObserved>? WordObserved;

    public bool PersonalDictionaryEnabled { get; set; }

    public bool SuggestionsEnabled { get; set; }

    public bool AutoApplySuggestions { get; set; }

    public bool ImmediateLayoutCorrect { get; set; }

    public bool AutoExpandEnabled { get; set; }

    public Func<string, bool>? IsCompleteTrigger { get; set; }

    public Func<string, bool>? IsKnownTrigger { get; set; }

    public AppBlocklist Blocklist { get; set; } = new(AppBlocklist.Defaults);

    public void UpdateBindings(IReadOnlyDictionary<HotkeyAction, Hotkey> bindings) => _bindings = bindings;

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        var moduleHandle = NativeMethods.GetModuleHandle("user32");
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, moduleHandle, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to install the low-level keyboard hook. The conversion hotkey will be unavailable.");
        }

        // A mouse hook lets us drop the as-you-type word buffer when the user clicks elsewhere, so an
        // automatic replacement can never fire against text at a caret position we didn't track.
        _mouseHookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        if (_mouseHookHandle == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to install the low-level mouse hook; as-you-type buffer won't reset on click.");
        }

        // Watch focus changes so the caret's surroundings are read the moment it moves — long before
        // the first keystroke of the next word, which is when we must decide about that keystroke.
        //
        // Two narrow hooks, not one wide range: SetWinEventHook(min, max) subscribes to *every* event id
        // in between, and 0x0003..0x8005 spans hundreds of them (window creation, every location change).
        const uint flags = NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS;

        _foregroundHookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _focusProc, 0, 0, flags);

        _focusHookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_FOCUS, NativeMethods.EVENT_OBJECT_FOCUS,
            IntPtr.Zero, _focusProc, 0, 0, flags);

        if (_focusHookHandle == IntPtr.Zero || _foregroundHookHandle == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to watch focus changes; the first word after a click may not be corrected.");
        }
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        if (_mouseHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }

        if (_focusHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_focusHookHandle);
            _focusHookHandle = IntPtr.Zero;
        }

        if (_foregroundHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_foregroundHookHandle);
            _foregroundHookHandle = IntPtr.Zero;
        }
    }

    // Focus moved to another control or window: the word we were tracking is gone, and the caret now
    // sits after a character we have never seen. Find out what it is while the user is still reaching
    // for the keyboard.
    private void FocusCallback(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        try
        {
            if (eventType is not (NativeMethods.EVENT_SYSTEM_FOREGROUND or NativeMethods.EVENT_OBJECT_FOCUS))
            {
                return;
            }

            // idObject == OBJID_CLIENT(-4) or OBJID_WINDOW(0); anything else is a menu, a caret, a
            // scrollbar — not the thing that owns the text.
            if (idObject is not (0 or -4))
            {
                return;
            }

            ResetWord(precedingKnown: false);
            _lastSuggestedWord = string.Empty;
            _lastForeground = NativeMethods.GetForegroundWindow();
            _undoArmed = false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Focus-change callback failed.");
        }
    }

    // Any mouse click can move the caret without a keystroke — invalidate the tracked word so we
    // never backspace/replace at the wrong location.
    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;
                if (msg is NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_RBUTTONDOWN or NativeMethods.WM_MBUTTONDOWN)
                {
                    ResetWord(precedingKnown: false);

                    // The caret moved, so the corrected text is no longer behind it.
                    _undoArmed = false;
                }
                else if (msg == NativeMethods.WM_LBUTTONUP)
                {
                    // The button-down probe read the caret where it used to be — the app moves it while
                    // handling the click. Ask again now that it has landed.
                    ProbePrecedingCharacter();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mouse hook callback failed.");
        }

        return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && IsKeyDown(wParam))
            {
                var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                // Ignore the keystrokes we synthesize ourselves. Otherwise the text we type back (a
                // snippet expansion, a layout correction) feeds straight back into the word buffer and
                // the features correct/expand their own output.
                if (info.dwExtraInfo == NativeMethods.SelfInjectedTag)
                {
                    return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                }

                var vk = (int)info.vkCode;
                var mods = CurrentModifiers();

                var matched = false;
                foreach (var (action, hotkey) in _bindings)
                {
                    if (hotkey.VirtualKey != vk || hotkey.Modifiers != mods)
                    {
                        continue;
                    }

                    if (action == HotkeyAction.UndoCorrection)
                    {
                        // Only take the key when there is a correction to undo — otherwise the undo
                        // binding (Shift+Backspace by default) would stop deleting characters.
                        if (!_undoArmed)
                        {
                            break;
                        }

                        _undoArmed = false;
                        Raise(UndoCorrectionRequested);

                        // Swallow it: the app must not also receive the Backspace.
                        return 1;
                    }

                    Raise(EventFor(action));
                    matched = true;
                    break;
                }

                // Any real keystroke other than a bare modifier means the correction is no longer the
                // last thing that happened, so undoing it would rewrite the wrong text.
                if (!matched && !IsModifierKey(vk))
                {
                    _undoArmed = false;
                }

                // Never type on the user's behalf inside a terminal, a remote session or a game: the
                // keystrokes we synthesize would run commands, travel to another machine, or be read
                // as raw input. Explicit hotkeys above still work — those are a deliberate act.
                if (Blocklist.IsBlocked(_foregroundApp.GetProcessName()))
                {
                    ResetWord(precedingKnown: false);
                    _undoArmed = false;
                    return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                }

                // Track plain typing for as-you-type features (layout hint / auto-expand). Only when a
                // feature is on and no command modifier is held.
                if ((SuggestionsEnabled || AutoExpandEnabled || PersonalDictionaryEnabled) && !matched &&
                    (mods & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) == 0)
                {
                    // If focus moved to another window (e.g. Alt+Tab), the tracked word is stale and a
                    // word suggested in the previous app can be offered again here.
                    var foreground = NativeMethods.GetForegroundWindow();
                    if (foreground != _lastForeground)
                    {
                        ResetWord(precedingKnown: false);
                        _lastSuggestedWord = string.Empty;
                        _lastForeground = foreground;
                    }

                    if (UpdateWordBuffer(vk, (mods & HotkeyModifiers.Shift) != 0))
                    {
                        // We have taken this keystroke. Whatever it would have typed is carried in the
                        // payload we just raised, and the handler types it as part of the replacement —
                        // or types it back verbatim if it decides not to replace after all. Letting the
                        // key through as well would race that replacement: the character arrives at the
                        // target window after CallNextHookEx returns, which is *after* we have already
                        // sent our backspaces. That race is what the old fixed delay tried to paper over.
                        return 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keyboard hook callback failed.");
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // Raise on a thread-pool thread so the hook returns quickly; handlers marshal to UI.
    private void Raise(EventHandler? handler)
    {
        if (handler is not null)
        {
            ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, EventArgs.Empty));
        }
    }

    /// <summary>
    /// Feeds one keystroke into the as-you-type word buffer. Returns true when this keystroke must be
    /// swallowed because a replacement has been raised for it — the handler will type it back as part
    /// of (or instead of) the replacement.
    /// </summary>
    private bool UpdateWordBuffer(int vk, bool shift)
    {
        switch (vk)
        {
            case NativeMethods.VK_SPACE:
            case NativeMethods.VK_RETURN:
            case NativeMethods.VK_TAB:
                var completed = _wordAbandoned ? string.Empty : _wordBuffer.ToString();

                // At most one of the two may fire: both would replace the same text. An expansion wins,
                // because the user typed a trigger deliberately.
                RaiseWordObserved(completed);

                var swallow = RaiseSnippetWordCompleted(completed, vk) ||
                              EvaluateWord(completed, atSpace: vk == NativeMethods.VK_SPACE);

                // The delimiter is now the character before the next run, and it hosts no Thai mark.
                ResetWord(precedingKnown: true);
                return swallow;
            case NativeMethods.VK_BACK:
                if (_wordAbandoned || _wordBuffer.Length == 0)
                {
                    return false;
                }

                if (_wordBuffer.Length == _onScreen.Length)
                {
                    _wordBuffer.Length--;
                    _onScreen.Length--;
                }
                else
                {
                    // Some keystroke of this run was swallowed, so the buffer and the screen no longer
                    // line up character for character and we cannot say which one the Backspace removed.
                    ResetWord(precedingKnown: false);
                }

                return false;
        }

        var c = VkToChar(vk, shift);
        if (c == '\0')
        {
            // Navigation / other keys break the current word, and move the caret somewhere we cannot see.
            ResetWord(precedingKnown: false);
            return false;
        }

        if (_wordAbandoned)
        {
            return false; // still inside an over-long run
        }

        if (_wordBuffer.Length == MaxTrackedWordLength)
        {
            // Truncating here would desynchronise the buffer from the document: we would believe the
            // word is 32 characters long while the caret has many more behind it, and a replacement
            // would backspace over whatever happened to be there. Give up on this run instead.
            AbandonWord();
            return false;
        }

        // What this key will put on screen: itself under a latin layout, its Thai counterpart otherwise.
        var thaiLayout = NativeMethods.ForegroundLayoutIsThai();
        var produced = thaiLayout
            ? _converter.Convert(c.ToString(), ConversionDirection.EnglishToThai)
            : c.ToString();

        if (thaiLayout && produced.Length == 1)
        {
            var previous = PrecedingCharacter();
            if (previous is null)
            {
                // The caret landed somewhere we never watched and UI Automation could not tell us what
                // sits behind it. This key might attach to a consonant already in the document, or float
                // free — we can neither swallow it nor count it. Stop tracking until the next word break.
                AbandonWord();
                return false;
            }

            if (!ThaiInput.Accepts(previous.Value, produced[0]))
            {
                // The mark has nothing to attach to. A Win32 edit control refuses it; a control that
                // draws its own text accepts it. Swallow it so that neither does — and keep the letter in
                // the buffer, because it is still part of the word the user meant to type.
                _wordBuffer.Append(c);
                return true;
            }
        }

        _wordBuffer.Append(c);

        // The trigger keystroke is decided before it lands: if it fires a replacement we swallow it, so
        // _onScreen must still describe the document as the handler will find it.
        if (TryExpandCompletedTrigger(produced) || TryCorrectLayoutMidWord(produced))
        {
            return true;
        }

        _onScreen.Append(produced);
        return false;
    }

    /// <summary>
    /// The character the next keystroke would attach to: the last one this run put on screen, or — for
    /// the run's first keystroke — whatever precedes it in the document. Null when that is unknowable.
    /// </summary>
    private char? PrecedingCharacter()
    {
        if (_onScreen.Length > 0)
        {
            return _onScreen[^1];
        }

        if (_precedingKnown)
        {
            return '\0'; // the delimiter we typed; no Thai mark can attach to it
        }

        // The probe was started when focus moved, so it has normally answered long ago. If the user was
        // quicker than UI Automation, give it a moment — but only a moment: Windows unhooks a low-level
        // hook that takes too long to return, and that would silence every feature at once.
        for (var waited = 0; waited < PrecedingWaitMs; waited += PrecedingPollMs)
        {
            if (_precedingProbed != PrecedingUnknown)
            {
                break;
            }

            Thread.Sleep(PrecedingPollMs);
        }

        var probed = _precedingProbed;
        return probed == PrecedingUnknown ? null : (char)probed;
    }

    /// <summary>Stops tracking until the next real word break, when the caret is no longer accounted for.</summary>
    private void AbandonWord()
    {
        _wordBuffer.Clear();
        _onScreen.Clear();
        _wordAbandoned = true;
        _precedingKnown = false;
        _undoArmed = false;
    }

    /// <summary>Forgets the current word — the only safe state once we lose track of the caret.</summary>
    /// <param name="precedingKnown">
    /// True when we saw what sits before the new run — a delimiter we just typed, which no Thai mark can
    /// attach to. False when the caret moved somewhere we cannot see (a click, a navigation key, another
    /// window), in which case the run's first keystroke might legitimately attach to a consonant already
    /// in the document.
    /// </param>
    private void ResetWord(bool precedingKnown)
    {
        _wordBuffer.Clear();
        _onScreen.Clear();
        _wordAbandoned = false;
        _precedingKnown = precedingKnown;

        if (!precedingKnown)
        {
            ProbePrecedingCharacter();
        }
    }

    /// <summary>
    /// Asks UI Automation what sits before the caret, on a thread-pool thread — the call can take tens
    /// of milliseconds and Windows unhooks a low-level hook that is slow to return. The answer usually
    /// lands long before the user's next keystroke; if it does not, the run is abandoned instead.
    /// </summary>
    private void ProbePrecedingCharacter()
    {
        _precedingProbed = PrecedingUnknown;
        var generation = Interlocked.Increment(ref _precedingGeneration);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            var found = _caret.GetCharacterBeforeCaret();
            if (found is char c && Volatile.Read(ref _precedingGeneration) == generation)
            {
                _precedingProbed = c;
            }
        });
    }

    // Correct wrong-layout Thai the moment it is recognisable, without waiting for a space. The
    // handler also switches the input language to Thai, so the remainder of the word types correctly.
    private bool TryCorrectLayoutMidWord(string swallowed)
    {
        if (!SuggestionsEnabled || !AutoApplySuggestions || !ImmediateLayoutCorrect || _wordBuffer.Length < 3)
        {
            return false;
        }

        var handler = LayoutAutoCorrectRequested;
        if (handler is null)
        {
            return false;
        }

        var word = _wordBuffer.ToString();
        if (word == _lastSuggestedWord)
        {
            return false;
        }

        var payload = BuildCorrection(word, string.Empty, swallowed);
        if (payload is null)
        {
            return false;
        }

        _lastSuggestedWord = word;
        _wordBuffer.Clear();
        _onScreen.Clear();
        _undoArmed = true;
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
        return true;
    }

    /// <summary>
    /// Decides whether <paramref name="word"/> (the latin characters the physical keys represent) needs
    /// correcting, in either direction. Both directions are settled by dictionary lookup — see
    /// <see cref="LayoutDecider"/>. Returns null when nothing to do.
    ///
    /// <para>The count comes from <see cref="_onScreen"/>, which records what this run actually put in
    /// the document — never a prediction. The keystroke that triggered us has not landed (we swallow it),
    /// so it is not in that count; <paramref name="swallowed"/> is what it would have typed.</para>
    /// </summary>
    private LayoutCorrection? BuildCorrection(string word, string boundary, string swallowed)
    {
        var payload = _decider.Decide(word, NativeMethods.ForegroundLayoutIsThai(), boundary);
        return payload is null
            ? null
            : payload with { CharsToDelete = _onScreen.Length, SwallowedText = swallowed };
    }

    // Expand the moment the typed text forms a complete trigger — no space needed. The predicate is
    // an in-memory lookup, and triggers that prefix a longer trigger are excluded from it (those
    // still expand on a space/tab delimiter instead).
    private bool TryExpandCompletedTrigger(string swallowed)
    {
        if (!AutoExpandEnabled || _wordBuffer.Length < 2)
        {
            return false;
        }

        var matcher = IsCompleteTrigger;
        var handler = SnippetWordCompleted;
        if (matcher is null || handler is null)
        {
            return false;
        }

        var word = _wordBuffer.ToString();
        if (!matcher(word))
        {
            return false;
        }

        // Empty delimiter: only the trigger itself is deleted and replaced. The keystroke that completed
        // it is swallowed and never reached the document, so _onScreen already excludes it.
        var payload = new WordBoundary(word, string.Empty, _onScreen.Length, swallowed);
        _wordBuffer.Clear();
        _onScreen.Clear();
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
        return true;
    }

    // Raise the auto-expand probe for a finished word that we already know is a trigger. Returns true
    // when the delimiter must be swallowed — we only take it once the word is worth expanding, so an
    // ordinary space is never held hostage by a database lookup that will find nothing.
    private bool RaiseSnippetWordCompleted(string word, int boundaryVk)
    {
        if (!AutoExpandEnabled || word.Length < 2)
        {
            return false;
        }

        var handler = SnippetWordCompleted;
        var known = IsKnownTrigger;
        if (handler is null || known is null)
        {
            return false;
        }

        // Only single-character delimiters (space / tab). Enter is skipped because the newline it
        // inserts is 1 or 2 characters depending on the app, so we can't reliably count backspaces.
        var boundary = boundaryVk switch
        {
            NativeMethods.VK_TAB => "\t",
            NativeMethods.VK_SPACE => " ",
            _ => null
        };
        if (boundary is null || !known(word))
        {
            return false;
        }

        var payload = new WordBoundary(word, boundary, _onScreen.Length, boundary);
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
        return true;
    }

    // Report the finished word for the personal dictionary — as it stands on screen, in the language of
    // the layout it was typed on. The hook applies no policy: what may be counted, and whether anything
    // is written at all, is decided by the handler, off this thread.
    private void RaiseWordObserved(string word)
    {
        if (!PersonalDictionaryEnabled || word.Length == 0)
        {
            return;
        }

        var handler = WordObserved;
        if (handler is null)
        {
            return;
        }

        var thaiLayout = NativeMethods.ForegroundLayoutIsThai();
        var onScreen = thaiLayout ? _onScreen.ToString() : word;
        if (onScreen.Length == 0)
        {
            return;
        }

        var payload = new WordObserved(onScreen, thaiLayout);
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
    }

    // Returns true when the delimiter must be swallowed because an automatic correction was raised.
    private bool EvaluateWord(string word, bool atSpace)
    {
        if (!SuggestionsEnabled || word.Length < 2 || word == _lastSuggestedWord)
        {
            return false;
        }

        // Automatic replacement runs only on a space boundary, so we never disturb a line break.
        var autoApply = AutoApplySuggestions && atSpace;

        if (autoApply)
        {
            var correct = LayoutAutoCorrectRequested;
            // The space that closed the word is swallowed with it and re-inserted after the fix.
            var payload = correct is null ? null : BuildCorrection(word, " ", swallowed: " ");
            if (payload is null)
            {
                return false;
            }

            _lastSuggestedWord = word;
            _undoArmed = true;
            ThreadPool.QueueUserWorkItem(_ => correct!.Invoke(this, payload));
            return true;
        }

        // Hint-only mode: same dictionary decision, but we merely suggest instead of replacing. Nothing
        // is typed and no key is swallowed, so we ask the decider directly — none of the counting
        // BuildCorrection does applies here.
        var hint = _decider.Decide(word, NativeMethods.ForegroundLayoutIsThai(), string.Empty);
        if (hint is null)
        {
            return false;
        }

        _lastSuggestedWord = word;
        var suggest = LayoutSuggestionRaised;
        if (suggest is not null)
        {
            var payload = new LayoutSuggestion(hint.Original, hint.Suggestion);
            ThreadPool.QueueUserWorkItem(_ => suggest.Invoke(this, payload));
        }

        return false;
    }

    // Maps a virtual-key to the character it produces on a US-QWERTY layout (physical key), honouring
    // the Shift state so the buffer matches what was actually typed (triggers and layout conversion
    // both depend on the correct case/symbol). Returns '\0' for keys that break the current word.
    private static char VkToChar(int vk, bool shift)
    {
        if (vk is >= 0x41 and <= 0x5A) // letters
        {
            return (char)(shift ? vk : vk + 32);
        }

        if (vk is >= 0x30 and <= 0x39) // digit row
        {
            if (!shift)
            {
                return (char)vk;
            }

            return vk switch
            {
                0x31 => '!', 0x32 => '@', 0x33 => '#', 0x34 => '$', 0x35 => '%',
                0x36 => '^', 0x37 => '&', 0x38 => '*', 0x39 => '(', 0x30 => ')',
                _ => '\0'
            };
        }

        return vk switch
        {
            NativeMethods.VK_OEM_1 => shift ? ':' : ';',
            NativeMethods.VK_OEM_2 => shift ? '?' : '/',
            NativeMethods.VK_OEM_3 => shift ? '~' : '`',
            NativeMethods.VK_OEM_4 => shift ? '{' : '[',
            NativeMethods.VK_OEM_5 => shift ? '|' : '\\',
            NativeMethods.VK_OEM_6 => shift ? '}' : ']',
            NativeMethods.VK_OEM_7 => shift ? '"' : '\'',
            NativeMethods.VK_OEM_PLUS => shift ? '+' : '=',
            NativeMethods.VK_OEM_MINUS => shift ? '_' : '-',
            NativeMethods.VK_OEM_COMMA => shift ? '<' : ',',
            NativeMethods.VK_OEM_PERIOD => shift ? '>' : '.',
            _ => '\0'
        };
    }

    private EventHandler? EventFor(HotkeyAction action) => action switch
    {
        HotkeyAction.Convert => ConversionHotkeyPressed,
        HotkeyAction.Expand => ExpansionHotkeyPressed,
        HotkeyAction.Picker => PickerHotkeyPressed,
        HotkeyAction.Capture => CaptureHotkeyPressed,
        HotkeyAction.AiImprove => AiImproveHotkeyPressed,
        HotkeyAction.UndoCorrection => UndoCorrectionRequested,
        _ => null
    };

    private static bool IsKeyDown(IntPtr wParam)
    {
        var msg = (int)wParam;
        return msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
    }

    private static HotkeyModifiers CurrentModifiers()
    {
        var mods = HotkeyModifiers.None;
        if (IsDown(NativeMethods.VK_CONTROL)) mods |= HotkeyModifiers.Ctrl;
        if (IsDown(NativeMethods.VK_SHIFT)) mods |= HotkeyModifiers.Shift;
        if (IsDown(NativeMethods.VK_MENU)) mods |= HotkeyModifiers.Alt;
        if (IsDown(NativeMethods.VK_LWIN) || IsDown(NativeMethods.VK_RWIN)) mods |= HotkeyModifiers.Win;
        return mods;
    }

    private static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    // A modifier press alone must not disarm the undo — Shift goes down before Shift+Backspace.
    private static bool IsModifierKey(int vk) => vk is
        NativeMethods.VK_SHIFT or NativeMethods.VK_CONTROL or NativeMethods.VK_MENU or
        NativeMethods.VK_LWIN or NativeMethods.VK_RWIN or
        0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5; // L/R Shift, Ctrl, Alt

    public void Dispose() => Stop();
}
