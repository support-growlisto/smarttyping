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

    // Keep the delegates alive for the lifetime of the hooks (prevents GC of the callbacks).
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly NativeMethods.LowLevelKeyboardProc _mouseProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private IntPtr _mouseHookHandle = IntPtr.Zero;

    // Current action→hotkey bindings (defaults until UpdateBindings is called). Volatile swap.
    private IReadOnlyDictionary<HotkeyAction, Hotkey> _bindings = SettingsService.DefaultHotkeys;

    // As-you-type suggestion state (accessed only on the hook/UI thread — both low-level hooks are
    // called on the thread that installed them, so no locking is needed).
    private readonly StringBuilder _wordBuffer = new(48);
    private string _lastSuggestedWord = string.Empty;
    private IntPtr _lastForeground = IntPtr.Zero;

    // True from the moment a correction is applied until the user types anything else. Guards the
    // undo hotkey so it only fires when the correction really is the last thing that happened.
    private volatile bool _undoArmed;

    public WindowsKeyboardHook(ILogger<WindowsKeyboardHook> logger, IKeyboardLayoutConverter converter, LayoutDecider decider)
    {
        _logger = logger;
        _converter = converter;
        _decider = decider;
        _proc = HookCallback;
        _mouseProc = MouseCallback;
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

    public bool SuggestionsEnabled { get; set; }

    public bool AutoApplySuggestions { get; set; }

    public bool ImmediateLayoutCorrect { get; set; }

    public bool AutoExpandEnabled { get; set; }

    public Func<string, bool>? IsCompleteTrigger { get; set; }

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
                    _wordBuffer.Clear();

                    // The caret moved, so the corrected text is no longer behind it.
                    _undoArmed = false;
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

                // Track plain typing for as-you-type features (layout hint / auto-expand). Only when a
                // feature is on and no command modifier is held.
                if ((SuggestionsEnabled || AutoExpandEnabled) && !matched &&
                    (mods & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) == 0)
                {
                    // If focus moved to another window (e.g. Alt+Tab), the tracked word is stale and a
                    // word suggested in the previous app can be offered again here.
                    var foreground = NativeMethods.GetForegroundWindow();
                    if (foreground != _lastForeground)
                    {
                        _wordBuffer.Clear();
                        _lastSuggestedWord = string.Empty;
                        _lastForeground = foreground;
                    }

                    UpdateWordBuffer(vk, (mods & HotkeyModifiers.Shift) != 0);
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

    private void UpdateWordBuffer(int vk, bool shift)
    {
        switch (vk)
        {
            case NativeMethods.VK_SPACE:
            case NativeMethods.VK_RETURN:
            case NativeMethods.VK_TAB:
                var completed = _wordBuffer.ToString();
                RaiseSnippetWordCompleted(completed, vk);
                EvaluateWord(completed, atSpace: vk == NativeMethods.VK_SPACE);
                _wordBuffer.Clear();
                return;
            case NativeMethods.VK_BACK:
                if (_wordBuffer.Length > 0)
                {
                    _wordBuffer.Length--;
                }
                return;
        }

        var c = VkToChar(vk, shift);
        if (c == '\0')
        {
            _wordBuffer.Clear(); // navigation / other keys break the current word
        }
        else if (_wordBuffer.Length < 48)
        {
            _wordBuffer.Append(c);
            if (!TryExpandCompletedTrigger())
            {
                TryCorrectLayoutMidWord();
            }
        }
    }

    // Correct wrong-layout Thai the moment it is recognisable, without waiting for a space. The
    // handler also switches the input language to Thai, so the remainder of the word types correctly.
    private void TryCorrectLayoutMidWord()
    {
        if (!SuggestionsEnabled || !AutoApplySuggestions || !ImmediateLayoutCorrect || _wordBuffer.Length < 3)
        {
            return;
        }

        var handler = LayoutAutoCorrectRequested;
        if (handler is null)
        {
            return;
        }

        var word = _wordBuffer.ToString();
        if (word == _lastSuggestedWord)
        {
            return;
        }

        var payload = BuildCorrection(word, string.Empty);
        if (payload is null)
        {
            return;
        }

        _lastSuggestedWord = word;
        _wordBuffer.Clear();
        _undoArmed = true;
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
    }

    /// <summary>
    /// Decides whether <paramref name="word"/> (the latin characters the physical keys represent) needs
    /// correcting, in either direction. Both directions are settled by dictionary lookup — see
    /// <see cref="LayoutDecider"/>. Returns null when nothing to do.
    /// </summary>
    private LayoutCorrection? BuildCorrection(string word, string boundary) =>
        _decider.Decide(word, NativeMethods.ForegroundLayoutIsThai(), boundary);

    // Expand the moment the typed text forms a complete trigger — no space needed. The predicate is
    // an in-memory lookup, and triggers that prefix a longer trigger are excluded from it (those
    // still expand on a space/tab delimiter instead).
    private bool TryExpandCompletedTrigger()
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

        _wordBuffer.Clear();

        // Empty delimiter: only the trigger itself is deleted and replaced.
        var payload = new WordBoundary(word, string.Empty);
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
        return true;
    }

    // Raise the auto-expand probe for the finished word (the coordinator decides if it's a trigger).
    private void RaiseSnippetWordCompleted(string word, int boundaryVk)
    {
        if (!AutoExpandEnabled || word.Length < 2)
        {
            return;
        }

        var handler = SnippetWordCompleted;
        if (handler is null)
        {
            return;
        }

        // Only single-character delimiters (space / tab). Enter is skipped because the newline it
        // inserts is 1 or 2 characters depending on the app, so we can't reliably count backspaces.
        var boundary = boundaryVk switch
        {
            NativeMethods.VK_TAB => "\t",
            NativeMethods.VK_SPACE => " ",
            _ => null
        };
        if (boundary is null)
        {
            return;
        }

        var payload = new WordBoundary(word, boundary);
        ThreadPool.QueueUserWorkItem(_ => handler.Invoke(this, payload));
    }

    private void EvaluateWord(string word, bool atSpace)
    {
        if (!SuggestionsEnabled || word.Length < 2 || word == _lastSuggestedWord)
        {
            return;
        }

        // Automatic replacement runs only on a space boundary, so we never disturb a line break.
        var autoApply = AutoApplySuggestions && atSpace;

        if (autoApply)
        {
            var correct = LayoutAutoCorrectRequested;
            // The space that closed the word is deleted with it and re-inserted after the fix.
            var payload = correct is null ? null : BuildCorrection(word, " ");
            if (payload is null)
            {
                return;
            }

            _lastSuggestedWord = word;
            _undoArmed = true;
            ThreadPool.QueueUserWorkItem(_ => correct!.Invoke(this, payload));
            return;
        }

        // Hint-only mode: same dictionary decision, but we merely suggest instead of replacing.
        var hint = BuildCorrection(word, string.Empty);
        if (hint is null)
        {
            return;
        }

        _lastSuggestedWord = word;
        var suggest = LayoutSuggestionRaised;
        if (suggest is not null)
        {
            var payload = new LayoutSuggestion(hint.Original, hint.Suggestion);
            ThreadPool.QueueUserWorkItem(_ => suggest.Invoke(this, payload));
        }
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
