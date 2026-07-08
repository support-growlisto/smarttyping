using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Enums;

namespace SmartTyping.Application.Language;

/// <summary>
/// Application-level entry point for language conversion. Thin wrapper over the pure
/// <see cref="IKeyboardLayoutConverter"/> that the UI/hotkey handler calls. Kept separate so
/// future concerns (last-word buffering, direction override, telemetry) have a home.
/// </summary>
public sealed class LanguageConversionService
{
    private readonly IKeyboardLayoutConverter _converter;

    public LanguageConversionService(IKeyboardLayoutConverter converter)
    {
        _converter = converter;
    }

    /// <summary>Converts text, auto-detecting the direction from its content.</summary>
    public string ConvertAuto(string text) => _converter.ConvertAuto(text);

    /// <summary>Converts text in an explicit direction.</summary>
    public string Convert(string text, ConversionDirection direction) =>
        _converter.Convert(text, direction);
}
