using System.Collections.Concurrent;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;

namespace SmartTyping.Infrastructure.Language;

/// <summary>
/// <see cref="ILexicon"/> backed by the gzipped word lists embedded in this assembly, plus the words
/// the user has taught us. Loading takes a moment (≈375k words), so it happens once on a background
/// thread; until it finishes <see cref="IsReady"/> is false and the layout corrector does nothing.
///
/// <para>Bundled lists are read-only and never written to; learned words live in their own concurrent
/// sets (read from the keyboard-hook thread, written from the undo handler) and in the database.</para>
///
/// <para>Both bundled lists are public domain — see <c>assets/dict/README.md</c>.</para>
/// </summary>
public sealed class EmbeddedLexicon : ILexicon
{
    private const string ThaiResource = "SmartTyping.Infrastructure.words_th.txt.gz";
    private const string EnglishResource = "SmartTyping.Infrastructure.words_en.txt.gz";

    private readonly ILearnedWordRepository _learned;
    private readonly ILogger<EmbeddedLexicon> _logger;

    // Learned words are consulted on the hook thread while the undo handler adds to them.
    private readonly ConcurrentDictionary<string, byte> _learnedThai = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _learnedEnglish = new(StringComparer.OrdinalIgnoreCase);

    private HashSet<string>? _thai;
    private HashSet<string>? _english;
    private volatile bool _ready;

    public EmbeddedLexicon(ILearnedWordRepository learned, ILogger<EmbeddedLexicon> logger)
    {
        _learned = learned;
        _logger = logger;
        _ = Task.Run(LoadAsync);
    }

    public bool IsReady => _ready;

    public bool IsThaiWord(string word) =>
        _ready && (_thai!.Contains(word) || _learnedThai.ContainsKey(word));

    public bool IsEnglishWord(string word) =>
        _ready && (_english!.Contains(word) || _learnedEnglish.ContainsKey(word));

    public void Learn(string word, bool isThai)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        var target = isThai ? _learnedThai : _learnedEnglish;
        if (!target.TryAdd(word, 0))
        {
            return; // already known
        }

        // Persist without blocking the caller (the undo handler is on an input path).
        _ = Task.Run(async () =>
        {
            try
            {
                await _learned.AddAsync(new LearnedWord(word, isThai), DateTime.UtcNow);
                _logger.LogInformation("Learned {Language} word {Word}.", isThai ? "Thai" : "English", word);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist the learned word {Word}.", word);
            }
        });
    }

    private async Task LoadAsync()
    {
        try
        {
            var thai = ReadWords(ThaiResource, StringComparer.Ordinal);
            var english = ReadWords(EnglishResource, StringComparer.OrdinalIgnoreCase);

            foreach (var word in await _learned.GetAllAsync())
            {
                (word.IsThai ? _learnedThai : _learnedEnglish).TryAdd(word.Word, 0);
            }

            _thai = thai;
            _english = english;
            _ready = true;

            _logger.LogInformation(
                "Lexicon loaded: {Thai} Thai words, {English} English words, {Learned} learned.",
                thai.Count, english.Count, _learnedThai.Count + _learnedEnglish.Count);
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
