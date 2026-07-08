namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Performs an in-place text replacement in the foreground app: deletes the just-typed characters
/// behind the caret and types a replacement. Shared by the opt-in automatic layout correction and
/// automatic snippet expansion. Implemented in Infrastructure. Never throws across the boundary.
/// </summary>
public interface IInlineReplacer
{
    /// <summary>
    /// Deletes <paramref name="charsToDelete"/> characters behind the caret and inserts
    /// <paramref name="replacement"/>. If <paramref name="cursorOffset"/> is set, the caret is left
    /// at that index within the inserted text (for snippet <c>{cursor}</c> markers).
    /// </summary>
    Task<bool> ReplaceAsync(int charsToDelete, string replacement, int? cursorOffset = null);
}
