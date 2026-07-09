namespace SmartTyping.Application.Language;

/// <summary>
/// An automatic wrong-layout correction to apply in place.
/// </summary>
/// <param name="Original">The latin characters the user actually typed.</param>
/// <param name="Suggestion">The Thai text they meant.</param>
/// <param name="Boundary">
/// The delimiter that closed the word (" " or "\t"), or empty when the correction fires mid-word
/// (no space needed). It is deleted along with the word and re-inserted after the replacement.
/// </param>
/// <param name="ToThai">
/// Which way the fix goes: true when latin gibberish becomes Thai, false when Thai gibberish becomes
/// English. The keyboard is switched to that language so the rest of the word types correctly.
/// </param>
public sealed record LayoutCorrection(string Original, string Suggestion, string Boundary, bool ToThai);
