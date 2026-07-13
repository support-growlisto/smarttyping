using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

/// <summary>
/// The personal dictionary is the only feature that writes what the user types to disk, so what it
/// refuses to write matters more than what it writes. These are the rules that keep a password, a token
/// or a URL out of the database — they are asserted here rather than trusted to a code review.
/// </summary>
public sealed class PersonalDictionaryTests
{
    [Theory]
    [InlineData("คับ")]        // the deliberate misspelling — the whole point of the feature
    [InlineData("จ้าา")]
    [InlineData("ดนัย")]        // a name
    [InlineData("ฟ้า")]
    public void ThaiWordsAreCountable(string word) => Assert.True(PersonalDictionary.MayCount(word, isThai: true));

    [Theory]
    [InlineData("kubota")]
    [InlineData("dont")]
    [InlineData("don't")]       // an apostrophe is inside real English words
    public void EnglishWordsAreCountable(string word) => Assert.True(PersonalDictionary.MayCount(word, isThai: false));

    // A password, a token, an API key and a URL all look like this. None of them may ever be counted.
    [Theory]
    [InlineData("hunter2")]
    [InlineData("P@ssw0rd")]
    [InlineData("sk-abc123")]
    [InlineData("a1b2c3")]
    [InlineData("user@example")]
    [InlineData("192.168")]
    [InlineData("C:/secret")]
    public void AnythingWithADigitOrSymbolIsRefused(string word) =>
        Assert.False(PersonalDictionary.MayCount(word, isThai: false));

    [Fact]
    public void OverlongRunsAreRefused()
    {
        // Nobody types a 21-character word repeatedly by hand; a long unbroken run is a secret.
        Assert.False(PersonalDictionary.MayCount(new string('a', 21), isThai: false));
        Assert.True(PersonalDictionary.MayCount(new string('a', 20), isThai: false));
    }

    [Fact]
    public void SingleCharactersAreRefused()
    {
        Assert.False(PersonalDictionary.MayCount("a", isThai: false));
        Assert.False(PersonalDictionary.MayCount("ก", isThai: true));
    }

    [Fact]
    public void EmptyIsRefused() => Assert.False(PersonalDictionary.MayCount("", isThai: false));

    // Thai and latin must not leak into each other: Thai text counted as an English word (or the other
    // way round) would put it in the wrong vocabulary, and the corrector would act on it in the wrong
    // direction.
    [Fact]
    public void ALanguageOnlyAcceptsItsOwnLetters()
    {
        Assert.False(PersonalDictionary.MayCount("คับ", isThai: false));
        Assert.False(PersonalDictionary.MayCount("kubota", isThai: true));
        Assert.False(PersonalDictionary.MayCount("คับok", isThai: true));
    }

    // Thai digits are digits, not letters — the same reasoning as latin ones.
    [Fact]
    public void ThaiDigitsAreRefused() => Assert.False(PersonalDictionary.MayCount("ปี๒๕", isThai: true));
}
