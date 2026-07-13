using System.Collections.Concurrent;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Domain.Enums;

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
    private readonly IPersonalWordRepository _personal;
    private readonly IKeyboardLayoutConverter _converter;
    private readonly ILogger<EmbeddedLexicon> _logger;

    // Learned words are consulted on the hook thread while the undo handler adds to them.
    private readonly ConcurrentDictionary<string, byte> _learnedThai = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _learnedEnglish = new(StringComparer.OrdinalIgnoreCase);

    // The personal dictionary: words typed often enough to count as the user's own. Small (tens to
    // hundreds), so the fuzzy lookup can scan them linearly instead of maintaining another index.
    private readonly ConcurrentDictionary<string, byte> _personalThai = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _personalEnglish = new(StringComparer.OrdinalIgnoreCase);

    private HashSet<string>? _thai;
    private HashSet<string>? _english;

    // Words bucketed by length for the fuzzy lookup: only same-length candidates can match, and the
    // buckets keep a near-miss scan to a few thousand comparisons instead of hundreds of thousands.
    // Thai words are stored as the latin keys that produce them, because the typo happened on a key.
    private IReadOnlyDictionary<int, string[]>? _thaiAsLatinByLength;
    private IReadOnlyDictionary<int, string[]>? _englishByLength;

    private volatile bool _ready;

    public EmbeddedLexicon(
        ILearnedWordRepository learned,
        IPersonalWordRepository personal,
        IKeyboardLayoutConverter converter,
        ILogger<EmbeddedLexicon> logger)
    {
        _personal = personal;
        _learned = learned;
        _converter = converter;
        _logger = logger;
        _ = Task.Run(LoadAsync);
    }

    public bool IsReady => _ready;

    public bool IsThaiWord(string word) =>
        _ready && (_thai!.Contains(word) || _learnedThai.ContainsKey(word) || _personalThai.ContainsKey(word));

    public bool IsEnglishWord(string word) =>
        _ready && (_english!.Contains(word) || _learnedEnglish.ContainsKey(word) || _personalEnglish.ContainsKey(word));

    public bool IsNearThaiWord(string latinTyped, int budget) =>
        _ready && (HasNeighbour(_thaiAsLatinByLength!, latinTyped, budget, StringComparison.Ordinal)
                   || HasPersonalNeighbour(_personalThai, latinTyped, budget, thai: true));

    public bool IsNearEnglishWord(string typed, int budget) =>
        _ready && (HasNeighbour(_englishByLength!, typed, budget, StringComparison.OrdinalIgnoreCase)
                   || HasPersonalNeighbour(_personalEnglish, typed, budget, thai: false));

    // The personal dictionary is small enough to scan. Thai entries are stored as Thai, but the typo
    // happened on a physical key, so they are compared as the latin that types them — same as the
    // bundled index.
    private bool HasPersonalNeighbour(ConcurrentDictionary<string, byte> words, string typed, int budget, bool thai)
    {
        if (budget < 0 || typed.Length == 0 || words.IsEmpty)
        {
            return false;
        }

        foreach (var word in words.Keys)
        {
            var candidate = thai ? _converter.Convert(word, ConversionDirection.ThaiToEnglish) : word;
            if (candidate.Length != typed.Length)
            {
                continue;
            }

            if (KeyboardCost.Distance(typed, candidate, budget) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNeighbour(
        IReadOnlyDictionary<int, string[]> byLength, string typed, int budget, StringComparison comparison)
    {
        if (budget < 0 || typed.Length == 0 || !byLength.TryGetValue(typed.Length, out var candidates))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (typed.Equals(candidate, comparison))
            {
                return true; // exact hit costs nothing
            }

            if (KeyboardCost.Distance(typed, candidate, budget) >= 0)
            {
                return true;
            }
        }

        return false;
    }

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

    public IReadOnlyList<LearnedEntry> LearnedWords =>
        _learnedThai.Keys.Select(w => new LearnedEntry(w, true))
            .Concat(_learnedEnglish.Keys.Select(w => new LearnedEntry(w, false)))
            .OrderBy(e => e.Word, StringComparer.CurrentCulture)
            .ToList();

    public void Forget(string word, bool isThai)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        var target = isThai ? _learnedThai : _learnedEnglish;
        if (!target.TryRemove(word, out _))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _learned.RemoveAsync(new LearnedWord(word, isThai));
                _logger.LogInformation("Forgot {Language} word {Word}.", isThai ? "Thai" : "English", word);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to forget the learned word {Word}.", word);
            }
        });
    }

    public void ForgetAll()
    {
        _learnedThai.Clear();
        _learnedEnglish.Clear();

        _ = Task.Run(async () =>
        {
            try
            {
                await _learned.ClearAsync();
                _logger.LogInformation("Forgot every learned word.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear the learned words.");
            }
        });
    }

    public void AddPersonal(string word, bool isThai)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        // Only the in-memory vocabulary: the tally on disk is the coordinator's, and it is already there.
        (isThai ? _personalThai : _personalEnglish).TryAdd(word, 0);
    }

    public void RemovePersonal(string word, bool isThai)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        (isThai ? _personalThai : _personalEnglish).TryRemove(word, out _);

        _ = Task.Run(async () =>
        {
            try
            {
                await _personal.RemoveAsync(word, isThai);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the personal word {Word}.", word);
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

            // Only words that reached the threshold are vocabulary; the rest are still just a tally.
            foreach (var word in await _personal.GetAllAsync())
            {
                if (word.Count >= PersonalDictionary.Threshold)
                {
                    (word.IsThai ? _personalThai : _personalEnglish).TryAdd(word.Word, 0);
                }
            }

            // Index for the fuzzy lookup. Thai words are keyed by the latin characters that type them.
            _thaiAsLatinByLength = BucketByLength(
                thai.Select(word => _converter.Convert(word, ConversionDirection.ThaiToEnglish)));
            _englishByLength = BucketByLength(english);

            _thai = thai;
            _english = english;
            _ready = true;

            _logger.LogInformation(
                "Lexicon loaded: {Thai} Thai words, {English} English words, {Learned} learned, {Personal} personal.",
                thai.Count, english.Count, _learnedThai.Count + _learnedEnglish.Count,
                _personalThai.Count + _personalEnglish.Count);
        }
        catch (Exception ex)
        {
            // Without the dictionaries the layout corrector stays inert; everything else still works.
            _logger.LogError(ex, "Failed to load the bundled word lists; layout auto-correction is disabled.");
        }
    }

    private static IReadOnlyDictionary<int, string[]> BucketByLength(IEnumerable<string> words) =>
        words.Where(word => word.Length > 0)
             .GroupBy(word => word.Length)
             .ToDictionary(group => group.Key, group => group.ToArray());

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
