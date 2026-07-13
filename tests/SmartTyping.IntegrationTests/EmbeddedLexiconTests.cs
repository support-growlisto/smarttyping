using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Infrastructure.Language;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>Exercises the real embedded word lists, not a stub.</summary>
public sealed class EmbeddedLexiconTests
{
    /// <summary>In-memory stand-in for the learned-word table.</summary>
    private sealed class FakeLearnedWords : ILearnedWordRepository
    {
        public List<LearnedWord> Words { get; } = new();

        public Task<IReadOnlyList<LearnedWord>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<LearnedWord>>(Words.ToList());

        public Task AddAsync(LearnedWord word, DateTime learnedUtc)
        {
            if (!Words.Contains(word))
            {
                Words.Add(word);
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(LearnedWord word)
        {
            Words.Remove(word);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Words.Clear();
            return Task.CompletedTask;
        }
    }

    private static EmbeddedLexicon LoadedLexicon(
        ILearnedWordRepository? learned = null, IPersonalWordRepository? personal = null)
    {
        var lexicon = new EmbeddedLexicon(
            learned ?? new FakeLearnedWords(),
            personal ?? new InMemoryPersonalWords(),
            new KeyboardLayoutConverter(),
            NullLogger<EmbeddedLexicon>.Instance);

        // Loading runs on a background thread; give it a bounded wait rather than sleeping blindly.
        var clock = Stopwatch.StartNew();
        while (!lexicon.IsReady && clock.Elapsed < TimeSpan.FromSeconds(30))
        {
            Thread.Sleep(25);
        }

        Assert.True(lexicon.IsReady, "The embedded word lists did not load.");
        return lexicon;
    }

    [Theory]
    [InlineData("หนังสือ")]
    [InlineData("สวัสดี")]
    [InlineData("ทดสอบ")]
    [InlineData("ไทย")]
    [InlineData("น้ำ")]
    public void RealThaiWords_AreFound(string word) => Assert.True(LoadedLexicon().IsThaiWord(word));

    [Theory]
    [InlineData("กนืงะ")]   // what "don't" produces on the Thai layout
    [InlineData("ไนพสก")]   // what "world" produces
    [InlineData("้ำสสน")]   // what "hello" produces
    public void ThaiGibberish_IsNotAWord(string word) => Assert.False(LoadedLexicon().IsThaiWord(word));

    [Theory]
    [InlineData("hello")]
    [InlineData("world")]
    [InlineData("HELLO")] // case-insensitive
    public void RealEnglishWords_AreFound(string word) => Assert.True(LoadedLexicon().IsEnglishWord(word));

    [Theory]
    [InlineData("l;ylfu")]  // สวัสดี typed on a latin layout
    [InlineData("soy'lnv")] // หนังสือ
    [InlineData("mflvb")]   // ทดสอบ
    public void LatinGibberish_IsNotAnEnglishWord(string word) => Assert.False(LoadedLexicon().IsEnglishWord(word));

    [Fact]
    public async Task LearnedWord_IsRecognisedImmediately_AndPersisted()
    {
        var store = new FakeLearnedWords();
        var lexicon = LoadedLexicon(store);

        Assert.False(lexicon.IsEnglishWord("soy'lnv"));

        lexicon.Learn("soy'lnv", isThai: false);

        // Visible to the hook thread straight away, without waiting for the write.
        Assert.True(lexicon.IsEnglishWord("soy'lnv"));

        // ...and it reaches the repository (persisted on a background task).
        var clock = Stopwatch.StartNew();
        while (store.Words.Count == 0 && clock.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(20);
        }

        Assert.Equal(new LearnedWord("soy'lnv", false), Assert.Single(store.Words));
    }

    [Fact]
    public void LearnedWords_AreLoadedOnStartup()
    {
        var store = new FakeLearnedWords();
        store.Words.Add(new LearnedWord("มั่ง", true));
        store.Words.Add(new LearnedWord("zzqq", false));

        var lexicon = LoadedLexicon(store);

        Assert.True(lexicon.IsThaiWord("มั่ง"));
        Assert.True(lexicon.IsEnglishWord("zzqq"));
        Assert.False(lexicon.IsThaiWord("zzqq")); // learning is per-language
    }
}
