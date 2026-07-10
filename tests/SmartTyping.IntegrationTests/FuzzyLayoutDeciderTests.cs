using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Infrastructure.Language;
using Xunit;
using Xunit.Abstractions;

namespace SmartTyping.IntegrationTests;

/// <summary>
/// Typo tolerance against the real dictionaries. A five-word stub cannot show whether a fuzzy match
/// is safe: the danger only appears with 315k English and 60k Thai words to collide with.
/// </summary>
public sealed class FuzzyLayoutDeciderTests
{
    private sealed class NoLearnedWords : ILearnedWordRepository
    {
        public Task<IReadOnlyList<LearnedWord>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<LearnedWord>>([]);

        public Task AddAsync(LearnedWord word, DateTime learnedUtc) => Task.CompletedTask;
    }

    private readonly ITestOutputHelper _output;
    private readonly LayoutDecider _decider;
    private readonly EmbeddedLexicon _lexicon;

    public FuzzyLayoutDeciderTests(ITestOutputHelper output)
    {
        _output = output;
        _lexicon = new EmbeddedLexicon(new NoLearnedWords(), new KeyboardLayoutConverter(), NullLogger<EmbeddedLexicon>.Instance);

        var clock = Stopwatch.StartNew();
        while (!_lexicon.IsReady && clock.Elapsed < TimeSpan.FromSeconds(30))
        {
            Thread.Sleep(25);
        }

        Assert.True(_lexicon.IsReady, "The embedded word lists did not load.");
        _decider = new LayoutDecider(_lexicon, new KeyboardLayoutConverter());
    }

    private LayoutCorrection? AtBoundary(string typed, bool thaiLayout = false) =>
        _decider.Decide(typed, thaiLayout, boundary: " ");

    [Theory]
    [InlineData("l;yldu")]  // สวัสดี with 'd' for 'f' — the key next door
    [InlineData("l;ylf7")]  // 'u' -> '7', the row above
    public void TypoedThai_IsStillRecognised(string typed)
    {
        var result = AtBoundary(typed);

        Assert.NotNull(result);
        Assert.True(result!.ToThai);

        // The layout is fixed, the spelling is not: we emit exactly the keys they pressed, in Thai.
        Assert.Equal(new KeyboardLayoutConverter().Convert(typed, Domain.Enums.ConversionDirection.EnglishToThai),
            result.Suggestion);
    }

    /// <summary>
    /// The one that matters. Ordinary English must survive a fuzzy Thai lookup over 60k words.
    /// </summary>
    [Theory]
    [InlineData("hello")]
    [InlineData("world")]
    [InlineData("people")]
    [InlineData("system")]
    [InlineData("number")]
    [InlineData("please")]
    [InlineData("thanks")]
    [InlineData("update")]
    [InlineData("commit")]
    [InlineData("branch")]
    [InlineData("server")]
    [InlineData("client")]
    public void CommonEnglishWords_AreNeverConvertedToThai(string word) => Assert.Null(AtBoundary(word));

    [Theory]
    [InlineData("wrold")]   // transposed "world" — two unrelated substitutions
    [InlineData("teh")]     // transposed "the"
    public void TransposedEnglish_IsNotReadAsThai(string word) => Assert.Null(AtBoundary(word));

    [Fact]
    public void RealThaiTypedOnTheThaiLayout_IsNeverRewritten()
    {
        // ทดสอบ (mflv[) and สวัสดี (l;ylfu) typed correctly with the Thai layout active.
        Assert.Null(AtBoundary("mflv[", thaiLayout: true));
        Assert.Null(AtBoundary("l;ylfu", thaiLayout: true));
    }

    [Fact]
    public void FuzzyIsOnlyConsultedAtAWordBoundary()
    {
        Assert.NotNull(_decider.Decide("l;yldu", thaiLayoutActive: false, boundary: " "));
        Assert.Null(_decider.Decide("l;yldu", thaiLayoutActive: false, boundary: ""));
    }

    [Fact]
    public void TheLengthCapDoesNotHideEnglishWords()
    {
        // Justifies MaximumWordLength: nothing we could match is longer than it.
        var typed = new string('a', LayoutDecider.MaximumWordLength + 1);
        Assert.Null(AtBoundary(typed));

        // The English list is filtered to <=12 characters, well inside the cap.
        Assert.True(_lexicon.IsEnglishWord("performance"));
    }

    [Fact]
    public void ALongRunIsNeverConverted()
    {
        // A password-shaped run, ending in something that *is* a Thai word ("l;ylfu" = สวัสดี).
        // Acting on it would rewrite the tail of text we never tracked from the start.
        var run = new string('x', 30) + "l;ylfu";
        Assert.True(run.Length > LayoutDecider.MaximumWordLength);

        Assert.Null(AtBoundary(run));
    }

    /// <summary>
    /// The decision runs inside the low-level keyboard hook, and Windows silently unhooks a callback
    /// that is too slow. A miss is the worst case: it scans the whole same-length bucket.
    /// </summary>
    [Fact]
    public void AMissIsFastEnoughForTheKeyboardHook()
    {
        const string worstCase = "zzzzzz"; // no Thai or English word is near this
        Assert.Null(AtBoundary(worstCase));

        var clock = Stopwatch.StartNew();
        const int iterations = 50;
        for (var i = 0; i < iterations; i++)
        {
            AtBoundary(worstCase);
        }

        var perCall = clock.Elapsed.TotalMilliseconds / iterations;
        _output.WriteLine($"worst-case fuzzy miss: {perCall:F2} ms per call");

        // Windows' default LowLevelHooksTimeout is 300ms; stay an order of magnitude below it.
        Assert.True(perCall < 25, $"Fuzzy miss took {perCall:F2} ms — too slow for the hook thread.");
    }
}
