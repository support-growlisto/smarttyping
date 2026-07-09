using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

public sealed class WrongLayoutDetectorTests
{
    [Theory]
    [InlineData("l;ylfu", true)]   // สวัสดี typed on QWERTY — has ';'
    [InlineData("[b'k", true)]     // has [ and '
    [InlineData("hello", false)]   // ordinary English word
    [InlineData("world", false)]
    [InlineData("a", false)]       // too short
    [InlineData("", false)]
    [InlineData("don't", true)]    // apostrophe present (accepted false-positive-ish; suggestion only)
    public void LooksLikeWrongLayoutThai(string word, bool expected)
    {
        Assert.Equal(expected, WrongLayoutDetector.LooksLikeWrongLayoutThai(word));
    }

    [Fact]
    public void RequiresBothLetterAndThaiConsonantPunctuation()
    {
        Assert.False(WrongLayoutDetector.LooksLikeWrongLayoutThai(";;;;"));  // punctuation only
        Assert.False(WrongLayoutDetector.LooksLikeWrongLayoutThai("abcd"));  // letters only
        Assert.True(WrongLayoutDetector.LooksLikeWrongLayoutThai("ab;cd"));  // both
    }

    [Theory]
    // English typed while the Thai layout was active — the Thai on screen is structurally impossible.
    [InlineData("้ำสสน", true)]   // "hello" -> starts with a tone mark
    [InlineData("ะ้ำ", true)]      // "the"   -> starts with a standalone vowel
    // Correctly typed Thai must never be flagged.
    [InlineData("สวัสดี", false)]
    [InlineData("ทดสอบ", false)]
    [InlineData("น้ำ", false)]     // tone mark on a consonant is fine
    [InlineData("ที่", false)]      // tone mark on a vowel is fine
    [InlineData("ไทย", false)]     // leading vowel is a valid start
    [InlineData("hello", false)]   // not Thai at all
    [InlineData("ก", false)]       // too short
    public void LooksLikeWrongLayoutEnglish(string thai, bool expected)
    {
        Assert.Equal(expected, WrongLayoutDetector.LooksLikeWrongLayoutEnglish(thai));
    }

    [Theory]
    [InlineData("don't", false)]   // apostrophe-only: NOT auto-corrected (English contraction)
    [InlineData("it's", false)]
    [InlineData("l;ylfu", true)]   // ';' still triggers strict mode
    [InlineData("[b'k", true)]     // has '[' so still flagged even though it also has '
    public void StrictModeIgnoresApostrophe(string word, bool expected)
    {
        Assert.Equal(expected, WrongLayoutDetector.LooksLikeWrongLayoutThai(word, strict: true));
    }
}
