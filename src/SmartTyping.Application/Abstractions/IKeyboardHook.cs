namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Abstraction over a global keyboard hook / hotkey source. Implemented in Infrastructure.
/// In the MVP this is passive: it observes input and raises the conversion hotkey; it never
/// rewrites keystrokes (no automatic correction).
/// </summary>
public interface IKeyboardHook : IDisposable
{
    /// <summary>Raised when the language-conversion hotkey (default Ctrl+Shift+L) is pressed.</summary>
    event EventHandler? ConversionHotkeyPressed;

    /// <summary>Raised when the snippet-expansion hotkey (default Ctrl+Shift+E) is pressed.</summary>
    event EventHandler? ExpansionHotkeyPressed;

    /// <summary>Raised when the quick-picker hotkey (default Ctrl+Shift+Space) is pressed.</summary>
    event EventHandler? PickerHotkeyPressed;

    /// <summary>Raised when the capture-to-snippet hotkey (default Ctrl+Shift+N) is pressed.</summary>
    event EventHandler? CaptureHotkeyPressed;

    /// <summary>Raised when the AI-improve hotkey (default Ctrl+Shift+I) is pressed.</summary>
    event EventHandler? AiImproveHotkeyPressed;

    /// <summary>
    /// Raised when as-you-type suggestions are enabled and the last typed word looks like wrong-layout
    /// text that could be converted. Purely a hint — nothing is replaced automatically.
    /// </summary>
    event EventHandler<Language.LayoutSuggestion>? LayoutSuggestionRaised;

    /// <summary>
    /// Raised (instead of <see cref="LayoutSuggestionRaised"/>) when automatic correction is enabled
    /// and a wrong-layout word is finished with a space. The handler replaces it in place.
    /// </summary>
    event EventHandler<Language.LayoutCorrection>? LayoutAutoCorrectRequested;

    /// <summary>
    /// Raised when auto-expand is enabled and the user finishes a word with a delimiter. The handler
    /// checks whether the word is a snippet trigger and, if so, replaces it in place.
    /// </summary>
    event EventHandler<Language.WordBoundary>? SnippetWordCompleted;

    /// <summary>Enables/disables the non-destructive as-you-type layout suggestions.</summary>
    bool SuggestionsEnabled { get; set; }

    /// <summary>Enables/disables automatic snippet expansion as you type (no hotkey).</summary>
    bool AutoExpandEnabled { get; set; }

    /// <summary>
    /// Fast, synchronous test for "the text typed so far is a complete snippet trigger". Set by the
    /// auto-expand coordinator from an in-memory index; called on the hook thread after every
    /// keystroke, so it must not touch the database or block. When it returns true the trigger is
    /// expanded immediately, without waiting for a space.
    /// </summary>
    Func<string, bool>? IsCompleteTrigger { get; set; }

    /// <summary>
    /// When true (and <see cref="SuggestionsEnabled"/> is on), a detected wrong-layout word is
    /// corrected automatically on the next space instead of only being suggested.
    /// </summary>
    bool AutoApplySuggestions { get; set; }

    /// <summary>
    /// When true, an automatic correction fires as soon as the typed text is recognised as
    /// wrong-layout Thai — no space required. The handler also switches the input language to Thai so
    /// the rest of the word types correctly. Only enable when a Thai layout is installed.
    /// </summary>
    bool ImmediateLayoutCorrect { get; set; }

    /// <summary>Replaces the hotkey bindings the hook matches against (applied live).</summary>
    void UpdateBindings(IReadOnlyDictionary<Domain.Enums.HotkeyAction, Domain.ValueObjects.Hotkey> bindings);

    /// <summary>Begins listening for the hotkey.</summary>
    void Start();

    /// <summary>Stops listening.</summary>
    void Stop();
}
