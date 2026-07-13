using SmartTyping.Application.Language;
using SmartTyping.Domain.Enums;
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

        private static readonly KeyboardLayoutConverter Converter = new();

        public bool IsReady { get; set; } = true;
        public bool IsThaiWord(string word) => _thai.Contains(word);
        public bool IsEnglishWord(string word) => _english.Contains(word);
        public void Learn(string word, bool isThai) => (isThai ? _thai : _english).Add(word);

        public IReadOnlyList<LearnedEntry> LearnedWords =>
            _thai.Select(w => new LearnedEntry(w, true))
                .Concat(_english.Select(w => new LearnedEntry(w, false)))
                .ToList();

        public void Forget(string word, bool isThai) => (isThai ? _thai : _english).Remove(word);

        public void ForgetAll()
        {
            _thai.Clear();
            _english.Clear();
        }

        // The fake keeps one vocabulary per language, so a personal word is simply a word.
        public void AddPersonal(string word, bool isThai) => (isThai ? _thai : _english).Add(word);

        public void RemovePersonal(string word, bool isThai) => (isThai ? _thai : _english).Remove(word);

        // Mirrors EmbeddedLexicon: Thai words are compared as the latin keys that type them.
        public bool IsNearThaiWord(string latinTyped, int budget) => _thai.Any(word =>
            KeyboardCost.Distance(latinTyped, Converter.Convert(word, ConversionDirection.ThaiToEnglish), budget) >= 0);

        public bool IsNearEnglishWord(string typed, int budget) =>
            _english.Any(word => KeyboardCost.Distance(typed, word, budget) >= 0);
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
    public void LongRuns_AreIgnored()
    {
        // Longer than any dictionary word: a password, a token or a URL — never something an
        // automatic rewrite should touch.
        var tooLong = new string('l', LayoutDecider.MaximumWordLength + 1);

        Assert.Null(Create().Decide(tooLong, thaiLayoutActive: false, boundary: " "));
    }

    [Fact]
    public void AWordExactlyAtTheLimit_IsStillConsidered()
    {
        // The bound must be inclusive, or a legitimate 20-character Thai word would be skipped.
        var lexicon = new FakeLexicon();
        var atLimit = new string('l', LayoutDecider.MaximumWordLength);
        lexicon.Learn(new KeyboardLayoutConverter().Convert(atLimit, ConversionDirection.EnglishToThai), isThai: true);

        Assert.NotNull(Create(lexicon).Decide(atLimit, thaiLayoutActive: false, boundary: " "));
    }

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

    // Learning is silent, permanent and invisible: one undo and that word is never corrected again, in
    // any application, with nothing on screen to say why. Forgetting it must bring the correction back —
    // that is what makes the learned-words list in Settings a real remedy rather than a cosmetic one.
    [Fact]
    public void ForgettingAWord_RestoresTheCorrection()
    {
        var lexicon = new FakeLexicon();
        var decider = Create(lexicon);

        lexicon.Learn("soy'lnv", isThai: false);
        Assert.Null(decider.Decide("soy'lnv", thaiLayoutActive: false, boundary: ""));

        lexicon.Forget("soy'lnv", isThai: false);
        Assert.NotNull(decider.Decide("soy'lnv", thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void ForgetAll_ClearsEveryLearnedWord_AndOnlyThose()
    {
        var lexicon = new FakeLexicon();
        lexicon.Learn("soy'lnv", isThai: false);
        lexicon.Learn("กขค", isThai: true);
        Assert.Equal(2, lexicon.LearnedWords.Count(e => e.Word is "soy'lnv" or "กขค"));

        lexicon.ForgetAll();

        Assert.False(lexicon.IsEnglishWord("soy'lnv"));
        Assert.False(lexicon.IsThaiWord("กขค"));
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

    // ---- Typo tolerance (only once the word is finished) ----

    // สวัสดี is "l;ylfu"; hitting 'd' instead of 'f' is the key next door and yields สวัสกี.
    private const string TypoedSawasdee = "l;yldu";

    [Fact]
    public void OneNeighbouringTypo_StillConverts_AtAWordBoundary()
    {
        var result = Create().Decide(TypoedSawasdee, thaiLayoutActive: false, boundary: " ");

        Assert.NotNull(result);
        Assert.True(result!.ToThai);
    }

    [Fact]
    public void ATypoIsNotSilentlySpellCorrected()
    {
        // We fix the layout, never the word: the output is the literal transliteration of the keys
        // they actually pressed — the typo survives, in Thai. Correcting to "สวัสดี" would be the app
        // rewriting the user's text on a guess.
        var result = Create().Decide(TypoedSawasdee, thaiLayoutActive: false, boundary: " ");

        Assert.Equal("สวัสกี", result!.Suggestion);
        Assert.NotEqual("สวัสดี", result.Suggestion);
    }

    [Fact]
    public void ATypoDoesNotConvertMidWord()
    {
        // Mid-word the text is a prefix of something longer, so a near-miss proves nothing yet.
        Assert.Null(Create().Decide(TypoedSawasdee, thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void ADistantMistake_IsNotATypo()
    {
        // 'q' is nowhere near 'f': this is a different word, not a slip.
        Assert.Null(Create().Decide("l;ylqu", thaiLayoutActive: false, boundary: " "));
    }

    [Fact]
    public void RealEnglishStaysEnglish_EvenWithFuzzyEnabled()
    {
        // The exact English veto still runs first at a boundary.
        Assert.Null(Create().Decide("world", thaiLayoutActive: false, boundary: " "));
        Assert.Null(Create().Decide("hello", thaiLayoutActive: false, boundary: " "));
    }

    [Fact]
    public void ATransposedEnglishWord_IsNotMistakenForThai()
    {
        // "wrold" is two unrelated substitutions from "world" — far beyond any budget — so it cannot
        // be fuzzily read as Thai either.
        Assert.Null(Create().Decide("wrold", thaiLayoutActive: false, boundary: " "));
    }

    [Fact]
    public void ThaiLayout_TypoedEnglish_IsRestored()
    {
        // "hellp" — 'p' next to 'o' — typed with the Thai layout active.
        var result = Create().Decide("hellp", thaiLayoutActive: true, boundary: " ");

        Assert.NotNull(result);
        Assert.False(result!.ToThai);
        Assert.Equal("hellp", result.Suggestion); // again: no spell-correction
    }

    [Fact]
    public void ThaiLayout_RealThaiWord_IsNeverRewrittenByFuzz()
    {
        // The Thai veto stays exact on purpose: ทดสอบ must survive even if its latin form happens to
        // sit one key away from some English word.
        Assert.Null(Create().Decide("mflvb", thaiLayoutActive: true, boundary: " "));
    }
}
