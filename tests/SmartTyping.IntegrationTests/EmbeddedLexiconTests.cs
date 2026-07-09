using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Infrastructure.Language;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>Exercises the real embedded word lists, not a stub.</summary>
public sealed class EmbeddedLexiconTests
{
    private static EmbeddedLexicon LoadedLexicon()
    {
        var lexicon = new EmbeddedLexicon(NullLogger<EmbeddedLexicon>.Instance);

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
}
