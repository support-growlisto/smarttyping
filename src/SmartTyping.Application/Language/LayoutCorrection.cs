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
public sealed record LayoutCorrection(string Original, string Suggestion, string Boundary);
