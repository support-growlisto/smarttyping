using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

public sealed class ThaiInputTests
{
    [Theory]
    // "hello" on the Thai layout maps to ้ำสสน, but the tone and sara-am have no base consonant,
    // so Windows drops them and only สสน is inserted. Observed on a real machine.
    [InlineData("้ำสสน", "สสน")]
    // "the" -> ะ้ำ : all three are dependent marks with nothing to attach to.
    [InlineData("ะ้ำ", "")]
    // Correctly-formed Thai passes through untouched.
    [InlineData("สวัสดี", "สวัสดี")]
    [InlineData("หนังสือ", "หนังสือ")]
    [InlineData("น้ำ", "น้ำ")]      // tone on a consonant, then sara-am on the tone
    [InlineData("ที่", "ที่")]       // vowel on a consonant, then tone on the vowel
    [InlineData("ไนพสก", "ไนพสก")]  // leading vowel + consonants
    // Latin and digits are never filtered.
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    public void Filter(string thai, string expected) => Assert.Equal(expected, ThaiInput.Filter(thai));

    [Fact]
    public void AttachedVowelNeedsAConsonant()
    {
        Assert.Equal("", ThaiInput.Filter("ิ"));       // sara i alone
        Assert.Equal("กิ", ThaiInput.Filter("กิ"));    // on a consonant, fine
        Assert.Equal("เก", ThaiInput.Filter("เกิ")[..2]); // leading vowel is not a base for sara i
    }
}
