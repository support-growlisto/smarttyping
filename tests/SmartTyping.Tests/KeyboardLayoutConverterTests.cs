using SmartTyping.Application.Language;
using SmartTyping.Domain.Enums;
using Xunit;

namespace SmartTyping.Tests;

public sealed class KeyboardLayoutConverterTests
{
    private readonly KeyboardLayoutConverter _converter = new();

    [Fact] // KC-1
    public void Convert_EnglishToThai_ProducesThaiGreeting()
    {
        var result = _converter.Convert("l;ylfu", ConversionDirection.EnglishToThai);
        Assert.Equal("สวัสดี", result);
    }

    [Fact] // KC-2
    public void Convert_ThaiToEnglish_ProducesLatinKeys()
    {
        var result = _converter.Convert("สวัสดี", ConversionDirection.ThaiToEnglish);
        Assert.Equal("l;ylfu", result);
    }

    [Fact] // KC-3
    public void Convert_RoundTrip_PreservesOriginal()
    {
        const string original = "l;ylfu";
        var thai = _converter.Convert(original, ConversionDirection.EnglishToThai);
        var back = _converter.Convert(thai, ConversionDirection.ThaiToEnglish);
        Assert.Equal(original, back);
    }

    [Fact] // KC-4
    public void Convert_UnmappedCharacters_PassThrough()
    {
        // Space is absent from the layout table and is preserved as-is (a→ฟ, s→ห).
        var result = _converter.Convert("a s", ConversionDirection.EnglishToThai);
        Assert.Equal("ฟ ห", result);
    }

    [Fact] // KC-5
    public void DetectDirection_PredominantlyThai_ReturnsThaiToEnglish()
    {
        var direction = _converter.DetectDirection("สวัสดีครับ");
        Assert.Equal(ConversionDirection.ThaiToEnglish, direction);
    }

    [Fact]
    public void DetectDirection_PredominantlyLatin_ReturnsEnglishToThai()
    {
        var direction = _converter.DetectDirection("hello");
        Assert.Equal(ConversionDirection.EnglishToThai, direction);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _converter.Convert(string.Empty, ConversionDirection.EnglishToThai));
    }
}
