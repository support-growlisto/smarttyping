using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Enums;

namespace SmartTyping.Application.Language;

/// <summary>
/// Pure implementation of <see cref="IKeyboardLayoutConverter"/> over <see cref="KedmaneeLayout"/>.
/// Contains no I/O and no state, so it is trivially unit-testable and thread-safe.
/// </summary>
public sealed class KeyboardLayoutConverter : IKeyboardLayoutConverter
{
    public string Convert(string text, ConversionDirection direction)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var map = direction == ConversionDirection.EnglishToThai
            ? KedmaneeLayout.EnglishToThai
            : KedmaneeLayout.ThaiToEnglish;

        var buffer = new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            buffer[i] = map.TryGetValue(c, out var mapped) ? mapped : c;
        }

        return new string(buffer);
    }

    public ConversionDirection DetectDirection(string text)
    {
        // Count Thai vs. Latin letters. If the text is predominantly Thai, the user most likely
        // wants it turned back into the Latin keys they meant to type, and vice versa.
        var thai = 0;
        var latin = 0;
        foreach (var c in text)
        {
            if (IsThai(c))
            {
                thai++;
            }
            else if (IsLatinLetter(c))
            {
                latin++;
            }
        }

        return thai > latin ? ConversionDirection.ThaiToEnglish : ConversionDirection.EnglishToThai;
    }

    public string ConvertAuto(string text) => Convert(text, DetectDirection(text));

    private static bool IsThai(char c) => c >= '฀' && c <= '๿';

    private static bool IsLatinLetter(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}
