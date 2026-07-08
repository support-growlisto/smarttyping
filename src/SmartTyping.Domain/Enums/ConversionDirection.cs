namespace SmartTyping.Domain.Enums;

/// <summary>
/// Direction for keyboard-layout conversion between Thai (Kedmanee) and English (QWERTY).
/// </summary>
public enum ConversionDirection
{
    /// <summary>Interpret Latin keystrokes as their Thai-layout equivalents (e.g. <c>l;ylfu</c> → <c>สวัสดี</c>).</summary>
    EnglishToThai,

    /// <summary>Interpret Thai characters as the Latin keys that produced them (e.g. <c>สวัสดี</c> → <c>l;ylfu</c>).</summary>
    ThaiToEnglish
}
