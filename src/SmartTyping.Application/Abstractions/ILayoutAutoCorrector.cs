namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Performs an in-place layout correction on the foreground app: deletes the just-typed characters
/// and types the corrected replacement. Used by the opt-in automatic as-you-type correction.
/// Implemented in Infrastructure. Never throws across the boundary.
/// </summary>
public interface ILayoutAutoCorrector
{
    /// <summary>
    /// Deletes <paramref name="charsToDelete"/> characters behind the caret (the wrong-layout word
    /// plus its trailing space) and inserts <paramref name="replacement"/> in their place.
    /// </summary>
    Task<bool> ReplaceLastWordAsync(int charsToDelete, string replacement);
}
