namespace SmartTyping.Application.Language;

/// <summary>
/// A non-destructive hint that the word just typed looks like it was entered in the wrong keyboard
/// layout, and could be converted. The app only *suggests* — it never replaces text automatically.
/// </summary>
public sealed record LayoutSuggestion(string Original, string Suggestion);
