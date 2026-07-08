namespace SmartTyping.Application.Snippets;

/// <summary>
/// Outcome of attempting to expand a trigger.
/// </summary>
public sealed class ExpansionResult
{
    private ExpansionResult(bool matched, string? expandedText, int? snippetId, int? cursorOffset)
    {
        Matched = matched;
        ExpandedText = expandedText;
        SnippetId = snippetId;
        CursorOffset = cursorOffset;
    }

    /// <summary>True when an enabled snippet matched the trigger.</summary>
    public bool Matched { get; }

    /// <summary>The rendered replacement text (template variables applied). Null when no match.</summary>
    public string? ExpandedText { get; }

    public int? SnippetId { get; }

    /// <summary>Caret position within <see cref="ExpandedText"/> from a <c>{cursor}</c> marker, if any.</summary>
    public int? CursorOffset { get; }

    public static ExpansionResult Hit(int snippetId, string expandedText, int? cursorOffset = null) =>
        new(true, expandedText, snippetId, cursorOffset);

    public static ExpansionResult Miss() => new(false, null, null, null);
}
