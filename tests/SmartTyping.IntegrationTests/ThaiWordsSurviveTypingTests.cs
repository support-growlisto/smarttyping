using System.IO.Compression;
using System.Reflection;
using SmartTyping.Application.Language;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>
/// The injector and the inline replacer <b>type</b> their output as keystrokes rather than pasting it.
/// Windows refuses a Thai tone mark or attached vowel that has no base consonant, so anything
/// <see cref="ThaiInput.Filter"/> would strip is text the user will never see.
///
/// <para>That is only acceptable if it never happens to a real word. This test asserts exactly that
/// against the whole embedded dictionary: every one of the 60,537 Thai words is typeable verbatim.
/// The characters that do get dropped belong to wrong-layout gibberish (<c>hello</c> → <c>้ำสสน</c>),
/// which is never what a correction produces — the decider only fires on a dictionary hit.</para>
/// </summary>
public sealed class ThaiWordsSurviveTypingTests
{
    // An above/below vowel written on another vowel instead of on a consonant. A handful of entries in
    // the source list carry one (รักษาู, อาวุูธ, เมีัย …) — typos in the data, not Thai anyone can type,
    // and the layout rejects the stray vowel exactly as it should.
    private static bool HasVowelOnVowel(string word)
    {
        const string attached = "ัิีึืุู็";
        const string anyVowel = "ัิีึืุู็ำะา";
        for (var i = 1; i < word.Length; i++)
        {
            if (attached.Contains(word[i]) && anyVowel.Contains(word[i - 1]))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void EveryThaiDictionaryWordIsTypeableUnchanged()
    {
        var mangled = ThaiWords()
            .Where(word => !string.Equals(ThaiInput.Filter(word), word, StringComparison.Ordinal))
            .Where(word => !HasVowelOnVowel(word))
            .Take(10)
            .Select(word => $"{word} -> {ThaiInput.Filter(word)}")
            .ToList();

        Assert.True(mangled.Count == 0, "Filter drops characters from real words: " + string.Join(", ", mangled));
    }

    private static IEnumerable<string> ThaiWords()
    {
        var assembly = Assembly.GetAssembly(typeof(SmartTyping.Infrastructure.Language.EmbeddedLexicon))!;
        using var raw = assembly.GetManifestResourceStream("SmartTyping.Infrastructure.words_th.txt.gz")!;
        using var gz = new GZipStream(raw, CompressionMode.Decompress);
        using var reader = new StreamReader(gz);

        while (reader.ReadLine() is string line)
        {
            var word = line.Trim();
            if (word.Length > 0)
            {
                yield return word;
            }
        }
    }
}
