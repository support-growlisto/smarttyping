using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;

namespace SmartTyping.UI.Services;

/// <summary>
/// Builds the personal dictionary from what the user types: the words no bundled dictionary knows —
/// names, jargon, the deliberate misspellings people use with each other. A word that survives every
/// rule below and is typed <see cref="PersonalDictionary.Threshold"/> times joins the vocabulary, and
/// the corrector can then fix it when it is typed on the wrong layout.
///
/// <para><b>This is the only part of the app that writes what you type to disk.</b> Every gate is here,
/// in one place, so there is no second path to audit:</para>
/// <list type="bullet">
/// <item>the feature is off unless the user turned it on;</item>
/// <item>nothing is counted while a password field has focus;</item>
/// <item>nothing is counted in a blocked app (terminals, password managers, remote sessions);</item>
/// <item>nothing is counted that is not plainly a word — no digits, no symbols, nothing overlong, which
/// is what tokens, passwords and URLs look like;</item>
/// <item>nothing is counted that a dictionary already knows, so ordinary prose leaves no trace;</item>
/// <item>a word counted but not reaching the threshold is deleted after
/// <see cref="PersonalDictionary.CandidateLifetimeDays"/> days.</item>
/// </list>
/// </summary>
public sealed class PersonalDictionaryCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly ILexicon _lexicon;
    private readonly IPersonalWordRepository _words;
    private readonly ISecureInputDetector _secureInput;
    private readonly IForegroundApp _foregroundApp;
    private readonly ILogger<PersonalDictionaryCoordinator> _logger;

    public PersonalDictionaryCoordinator(
        IKeyboardHook hook,
        ILexicon lexicon,
        IPersonalWordRepository words,
        ISecureInputDetector secureInput,
        IForegroundApp foregroundApp,
        ILogger<PersonalDictionaryCoordinator> logger)
    {
        _hook = hook;
        _lexicon = lexicon;
        _words = words;
        _secureInput = secureInput;
        _foregroundApp = foregroundApp;
        _logger = logger;
    }

    /// <summary>Raised when a word has been typed often enough to join the vocabulary.</summary>
    public event EventHandler<string>? WordAdopted;

    public void Start()
    {
        _hook.WordObserved += OnWordObserved;
        _ = PruneAsync();
    }

    public void Stop() => _hook.WordObserved -= OnWordObserved;

    /// <summary>
    /// Drops the tallies of words typed once or twice and not seen since. A word the user typed in
    /// passing a month ago should not still be sitting on their disk.
    /// </summary>
    private async Task PruneAsync()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-PersonalDictionary.CandidateLifetimeDays);
            var dropped = await _words.PruneCandidatesAsync(PersonalDictionary.Threshold, cutoff);
            if (dropped > 0)
            {
                _logger.LogInformation("Pruned {Count} personal-dictionary candidate(s) older than {Days} days.",
                    dropped, PersonalDictionary.CandidateLifetimeDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to prune the personal-dictionary candidates.");
        }
    }

    private async void OnWordObserved(object? sender, WordObserved e)
    {
        try
        {
            if (!_hook.PersonalDictionaryEnabled)
            {
                return;
            }

            // A password field, or an app we never type into, is an app we never read from either.
            if (_secureInput.IsFocusedFieldSecure() || _hook.Blocklist.IsBlocked(_foregroundApp.GetProcessName()))
            {
                return;
            }

            if (!PersonalDictionary.MayCount(e.Word, e.IsThai))
            {
                return;
            }

            // A word a dictionary already knows needs no learning — and not counting it means ordinary
            // prose never reaches the disk at all.
            var known = e.IsThai ? _lexicon.IsThaiWord(e.Word) : _lexicon.IsEnglishWord(e.Word);
            if (known)
            {
                return;
            }

            var count = await _words.RecordAsync(e.Word, e.IsThai, DateTime.UtcNow);
            if (count != PersonalDictionary.Threshold)
            {
                return; // still a tally — or already adopted, and counted again
            }

            _lexicon.AddPersonal(e.Word, e.IsThai);
            _logger.LogInformation("Personal dictionary: adopted {Language} word after {Count} sightings.",
                e.IsThai ? "Thai" : "English", count);

            WordAdopted?.Invoke(this, e.Word);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to record an observed word.");
        }
    }

    public void Dispose() => Stop();
}
