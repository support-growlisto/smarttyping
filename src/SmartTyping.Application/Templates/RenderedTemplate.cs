namespace SmartTyping.Application.Templates;

/// <summary>
/// Result of rendering a snippet template: the resolved text plus an optional caret position
/// (the index within <see cref="Text"/> where a <c>{cursor}</c> marker was found).
/// </summary>
public sealed class RenderedTemplate
{
    public RenderedTemplate(string text, int? cursorOffset)
    {
        Text = text;
        CursorOffset = cursorOffset;
    }

    /// <summary>The rendered text.</summary>
    public string Text { get; }

    /// <summary>Index within <see cref="Text"/> to place the caret, or null if no marker was present.</summary>
    public int? CursorOffset { get; }

    public static RenderedTemplate Plain(string text) => new(text, null);
}
