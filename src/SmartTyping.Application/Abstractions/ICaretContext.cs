namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Reads the character immediately before the caret in whatever control has focus. Implemented in
/// Infrastructure over UI Automation, which reaches inside browsers, Electron apps and WPF windows —
/// not just native Win32 edit controls. Never throws.
///
/// <para>The keyboard hook needs this only after the caret has moved somewhere it did not watch (a
/// mouse click, a navigation key, another window). Until then it knows what precedes the word being
/// typed, because it saw the delimiter that ended the previous one.</para>
///
/// <para>Why it matters: a Thai tone mark or attached vowel typed as the first keystroke of a run is
/// either a stray mark that must be suppressed — or a mark the user is deliberately adding to the
/// consonant already sitting before the caret. Only the character before the caret can tell the two
/// apart, and guessing wrong either corrupts the text or silently eats a keystroke.</para>
/// </summary>
public interface ICaretContext
{
    /// <summary>
    /// The character before the caret; <c>'\0'</c> when the caret is at the very start of the text, and
    /// <c>null</c> when it could not be determined (no text pattern, no focus, a timeout, an error).
    /// May be slow — call it off the keyboard-hook thread.
    /// </summary>
    char? GetCharacterBeforeCaret();
}
