using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.Tests;

public sealed class LayoutDeciderTests
{
    // Only the words these tests need. The real dictionaries are much larger.
    private sealed class FakeLexicon : ILexicon
    {
        private readonly HashSet<string> _thai = new(StringComparer.Ordinal)
        {
            "หนังสือ", "หนัง", "สวัสดี", "ทดสอบ", "ไทย"
        };

        private readonly HashSet<string> _english = new(StringComparer.OrdinalIgnoreCase)
        {
            "hello", "world", "the", "soy", "sit", "don"
        };

        public bool IsReady { get; set; } = true;
        public bool IsThaiWord(string word) => _thai.Contains(word);
        public bool IsEnglishWord(string word) => _english.Contains(word);
        public void Learn(string word, bool isThai) => (isThai ? _thai : _english).Add(word);
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

    // ---- Learning: undoing a correction must stop it happening again ----

    [Fact]
    public void LearningTheOriginal_StopsTheSameCorrection_LatinLayout()
    {
        var lexicon = new FakeLexicon();
        var decider = Create(lexicon);

        // "soy'lnv" would normally become หนังสือ...
        Assert.NotNull(decider.Decide("soy'lnv", thaiLayoutActive: false, boundary: ""));

        // ...but the user undid it, so we learn the latin as an English word. The English veto now
        // blocks the conversion for good.
        lexicon.Learn("soy'lnv", isThai: false);
        Assert.Null(decider.Decide("soy'lnv", thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void LearningTheOriginal_StopsTheSameCorrection_ThaiLayout()
    {
        var lexicon = new FakeLexicon();
        var decider = Create(lexicon);

        // "hello" typed on the Thai layout shows สสน and would be restored to latin...
        var first = decider.Decide("hello", thaiLayoutActive: true, boundary: "");
        Assert.NotNull(first);

        // ...but the user meant that Thai. Learn what was on screen as a Thai word; the Thai veto wins.
        lexicon.Learn(first!.Original, isThai: true);
        Assert.Null(decider.Decide("hello", thaiLayoutActive: true, boundary: ""));
    }

    [Fact]
    public void LearningIsDirectional_AndDoesNotLeakAcrossLanguages()
    {
        var lexicon = new FakeLexicon();

        // Learning the latin as English must not make it a Thai word.
        lexicon.Learn("soy'lnv", isThai: false);
        Assert.True(lexicon.IsEnglishWord("soy'lnv"));
        Assert.False(lexicon.IsThaiWord("soy'lnv"));
    }

    [Fact]
    public void BoundaryIsCarriedThrough()
    {
        var result = Create().Decide("l;ylfu", thaiLayoutActive: false, boundary: " ");
        Assert.Equal(" ", result!.Boundary);
    }
}
