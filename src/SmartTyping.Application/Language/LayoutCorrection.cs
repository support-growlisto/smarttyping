namespace SmartTyping.Application.Language;

/// <summary>
/// An automatic wrong-layout correction to apply in place.
/// </summary>
/// <param name="Original">
/// What the user meant to leave on screen: the latin they typed, or the Thai the keys produced. Used
/// to undo the correction and to learn the word.
/// </param>
/// <param name="Suggestion">The text to type in its place.</param>
/// <param name="Boundary">
/// The delimiter that closed the word (" " or "\t"), or empty when the correction fires mid-word.
/// It is re-inserted after the replacement.
/// </param>
/// <param name="ToThai">
/// Which way the fix goes: true when latin gibberish becomes Thai, false when Thai gibberish becomes
/// English. The keyboard is switched to that language so the rest of the word types correctly.
/// </param>
/// <param name="CharsToDelete">
/// How many characters have actually reached the document and must be removed. The hook counts them —
/// it is the only place that knows which keystroke it swallowed, and that the Thai layout may have
/// silently rejected some of them.
/// </param>
/// <param name="SwallowedText">
/// What the swallowed keystroke would have produced. The hook takes that key so our replacement can
/// never race it; if the correction then fails, the handler types this back so the user loses nothing.
/// </param>
public sealed record LayoutCorrection(
    string Original,
    string Suggestion,
    string Boundary,
    bool ToThai,
    int CharsToDelete = 0,
    string SwallowedText = "");
