using System;
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

    // The keyboard hook counts what the Thai layout left on screen for the keys already delivered, then
    // takes the tail of the full word as "what the swallowed key would have typed". That subtraction is
    // only valid because Filter rejects left-to-right: filtering a prefix must give a prefix of
    // filtering the whole. If this ever stops holding, the hook deletes the wrong number of characters.
    [Theory]
    [InlineData("้ำสสน")]
    [InlineData("ะ้ำ")]
    [InlineData("สวัสดี")]
    [InlineData("น้ำ")]
    [InlineData("ที่")]
    [InlineData("ไนพสก")]
    [InlineData("่่้้๊๊")]
    [InlineData("กิิ")]
    [InlineData("abc123")]
    public void FilteringAPrefix_YieldsAPrefixOfFilteringTheWhole(string thai)
    {
        var full = ThaiInput.Filter(thai);

        for (var i = 0; i <= thai.Length; i++)
        {
            var partial = ThaiInput.Filter(thai[..i]);
            Assert.StartsWith(partial, full, StringComparison.Ordinal);
        }
    }
}
