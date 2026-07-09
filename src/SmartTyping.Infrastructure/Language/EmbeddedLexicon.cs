using System.IO.Compression;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Language;

namespace SmartTyping.Infrastructure.Language;

/// <summary>
/// <see cref="ILexicon"/> backed by the gzipped word lists embedded in this assembly. Loading takes a
/// moment (≈375k words), so it happens once on a background thread; until it finishes
/// <see cref="IsReady"/> is false and the layout corrector simply does nothing.
///
/// <para>Both lists are public domain — see <c>assets/dict/README.md</c>.</para>
/// </summary>
public sealed class EmbeddedLexicon : ILexicon
{
    private const string ThaiResource = "SmartTyping.Infrastructure.words_th.txt.gz";
    private const string EnglishResource = "SmartTyping.Infrastructure.words_en.txt.gz";

    private readonly ILogger<EmbeddedLexicon> _logger;

    private HashSet<string>? _thai;
    private HashSet<string>? _english;
    private volatile bool _ready;

    public EmbeddedLexicon(ILogger<EmbeddedLexicon> logger)
    {
        _logger = logger;
        _ = Task.Run(Load);
    }

    public bool IsReady => _ready;

    public bool IsThaiWord(string word) => _ready && _thai!.Contains(word);

    public bool IsEnglishWord(string word) => _ready && _english!.Contains(word);

    private void Load()
    {
        try
        {
            var thai = ReadWords(ThaiResource, StringComparer.Ordinal);
            var english = ReadWords(EnglishResource, StringComparer.OrdinalIgnoreCase);

            _thai = thai;
            _english = english;
            _ready = true;

            _logger.LogInformation("Lexicon loaded: {Thai} Thai words, {English} English words.",
                thai.Count, english.Count);
        }
        catch (Exception ex)
        {
            // Without the dictionaries the layout corrector stays inert; everything else still works.
            _logger.LogError(ex, "Failed to load the bundled word lists; layout auto-correction is disabled.");
        }
    }

    private static HashSet<string> ReadWords(string resource, StringComparer comparer)
    {
        using var stream = typeof(EmbeddedLexicon).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded resource '{resource}' is missing.");
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, System.Text.Encoding.UTF8);

        var words = new HashSet<string>(comparer);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                words.Add(line);
            }
        }

        return words;
    }
}
