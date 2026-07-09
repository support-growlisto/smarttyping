using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

public sealed class LayoutDeciderTests
{
    // Only the words these tests need. The real dictionaries are much larger.
    private sealed class FakeLexicon : ILexicon
    {
        private static readonly HashSet<string> Thai = new(StringComparer.Ordinal)
        {
            "หนังสือ", "หนัง", "สวัสดี", "ทดสอบ", "ไทย"
        };

        private static readonly HashSet<string> English = new(StringComparer.OrdinalIgnoreCase)
        {
            "hello", "world", "the", "soy", "sit", "don"
        };

        public bool IsReady { get; set; } = true;
        public bool IsThaiWord(string word) => Thai.Contains(word);
        public bool IsEnglishWord(string word) => English.Contains(word);
    }

    private static LayoutDecider Create(FakeLexicon? lexicon = null) =>
        new(lexicon ?? new FakeLexicon(), new KeyboardLayoutConverter());

    // ---- Latin layout: the user forgot to switch to Thai ----

    [Fact]
    public void LatinLayout_ThaiWordTyped_ConvertsToThai()
    {
        var result = Create().Decide("l;ylfu", thaiLayoutActive: false, boundary: "");

        Assert.NotNull(result);
        Assert.True(result!.ToThai);
        Assert.Equal("l;ylfu", result.Original);
        Assert.Equal("สวัสดี", result.Suggestion);
    }

    [Fact]
    public void LatinLayout_ApostropheIsThaiNgo_Converts()
    {
        // The apostrophe is 'ง'; this is the case a character-shape heuristic kept missing.
        var result = Create().Decide("soy'lnv", thaiLayoutActive: false, boundary: "");

        Assert.NotNull(result);
        Assert.Equal("หนังสือ", result!.Suggestion);
    }

    [Fact]
    public void LatinLayout_RealEnglishWord_IsLeftAlone()
    {
        // "world" maps to ไนพสก, which is not a Thai word — and "world" *is* English.
        Assert.Null(Create().Decide("world", thaiLayoutActive: false, boundary: ""));
        Assert.Null(Create().Decide("hello", thaiLayoutActive: false, boundary: ""));
        Assert.Null(Create().Decide("the", thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void LatinLayout_EnglishWordThatMapsToThaiWord_PrefersEnglish()
    {
        // The English veto: even if the Thai reading were a word, a real English word wins.
        var lexicon = new FakeLexicon();
        Assert.True(lexicon.IsEnglishWord("soy"));
        Assert.Null(Create(lexicon).Decide("soy", thaiLayoutActive: false, boundary: ""));
    }

    // ---- Thai layout: the user forgot to switch to English ----

    [Fact]
    public void ThaiLayout_EnglishWordTyped_RestoresLatinAndSwitchesBack()
    {
        var result = Create().Decide("hello", thaiLayoutActive: true, boundary: "");

        Assert.NotNull(result);
        Assert.False(result!.ToThai);
        Assert.Equal("hello", result.Suggestion);

        // The keys map to ้ำสสน, but the Thai layout rejects the leading tone and sara-am, so only
        // สสน reached the screen. Original must be what is really there — deleting five characters
        // would eat two of whatever preceded it.
        Assert.Equal("สสน", result.Original);
        Assert.NotEqual("hello".Length, result.Original.Length);
    }

    [Fact]
    public void ThaiLayout_RealThaiWord_IsNeverTouched()
    {
        // "mflvb" produces ทดสอบ, a real Thai word — the Thai veto stops us even though we'd
        // otherwise ask whether "mflvb" is English (it isn't).
        Assert.Null(Create().Decide("mflvb", thaiLayoutActive: true, boundary: ""));
        Assert.Null(Create().Decide("l;ylfu", thaiLayoutActive: true, boundary: ""));
    }

    // ---- Guards ----

    [Theory]
    [InlineData("l;")]
    [InlineData("ab")]
    public void ShortRuns_AreIgnored(string typed) =>
        Assert.Null(Create().Decide(typed, thaiLayoutActive: false, boundary: ""));

    [Fact]
    public void UnloadedLexicon_DoesNothing()
    {
        var lexicon = new FakeLexicon { IsReady = false };
        Assert.Null(Create(lexicon).Decide("l;ylfu", thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void BoundaryIsCarriedThrough()
    {
        var result = Create().Decide("l;ylfu", thaiLayoutActive: false, boundary: " ");
        Assert.Equal(" ", result!.Boundary);
    }
}
