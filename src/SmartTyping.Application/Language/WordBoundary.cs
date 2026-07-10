namespace SmartTyping.Application.Language;

/// <summary>
/// A word the user just finished typing. Raised by the keyboard hook once it has confirmed the word is
/// a snippet trigger, so the coordinator only has to render and inject it.
/// </summary>
/// <param name="Word">The raw typed word (physical QWERTY characters).</param>
/// <param name="Boundary">The delimiter to re-insert after the expansion (" " or "\t"), or empty.</param>
/// <param name="CharsToDelete">
/// How many characters of the trigger actually reached the document. The keystroke that completed it
/// was swallowed by the hook, so this is one fewer than the word length when it fired mid-word.
/// </param>
/// <param name="SwallowedText">
/// What the swallowed keystroke would have produced, so the coordinator can type it back if it ends up
/// not expanding after all (a secure field, or a cancelled <c>{input:…}</c> prompt).
/// </param>
public sealed record WordBoundary(
    string Word,
    string Boundary,
    int CharsToDelete = 0,
    string SwallowedText = "");
