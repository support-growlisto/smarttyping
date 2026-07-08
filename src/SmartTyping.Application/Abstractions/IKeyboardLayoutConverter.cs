using SmartTyping.Domain.Enums;

namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Pure converter between Thai (Kedmanee) and English (QWERTY) keyboard layouts.
/// No I/O; safe to call on any thread.
/// </summary>
public interface IKeyboardLayoutConverter
{
    /// <summary>Converts <paramref name="text"/> in the given <paramref name="direction"/>. Unmapped characters pass through.</summary>
    string Convert(string text, ConversionDirection direction);

    /// <summary>
    /// Heuristically determines which direction is likely intended for <paramref name="text"/>,
    /// based on the ratio of Thai vs. Latin characters.
    /// </summary>
    ConversionDirection DetectDirection(string text);

    /// <summary>Convenience: detects the direction and converts in one call.</summary>
    string ConvertAuto(string text);
}
