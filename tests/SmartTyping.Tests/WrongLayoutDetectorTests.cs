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
}
