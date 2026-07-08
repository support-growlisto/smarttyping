using SmartTyping.Application.Templates;

namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Renders template variables inside snippet content (e.g. <c>{date}</c>, <c>{time}</c>,
/// <c>{clipboard}</c>, <c>{cursor}</c>), with optional date/time formats and day offsets.
/// Unknown tokens are left untouched.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Replaces known variables in <paramref name="content"/> and returns the rendered text plus an
    /// optional caret position (from a <c>{cursor}</c> marker).
    /// </summary>
    Task<RenderedTemplate> RenderAsync(string content);
}
