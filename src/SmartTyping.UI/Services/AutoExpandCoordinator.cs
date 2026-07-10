using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Application.Snippets;

namespace SmartTyping.UI.Services;

/// <summary>
/// Automatic snippet expansion as you type (opt-in), with no hotkey:
/// <list type="bullet">
/// <item>the moment the typed text forms a complete trigger (e.g. <c>/sig</c>) it is replaced; and</item>
/// <item>triggers that are a prefix of a longer trigger instead expand on a space or tab.</item>
/// </list>
/// Skips detected password fields. Failures are logged and swallowed.
/// </summary>
public sealed class AutoExpandCoordinator : IDisposable
{
    // How often the in-memory trigger index is refreshed from the database. The index is consulted on
    // the keyboard-hook thread, so it can never hit the DB itself.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);

    private readonly IKeyboardHook _hook;
    private readonly SnippetExpansionService _expansion;
    private readonly ISnippetRepository _snippets;
    private readonly IInlineReplacer _replacer;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<AutoExpandCoordinator> _logger;

    private volatile TriggerIndex _index = TriggerIndex.Empty;
    private System.Threading.Timer? _refreshTimer;
    private int _busy;

    public AutoExpandCoordinator(
        IKeyboardHook hook,
        SnippetExpansionService expansion,
        ISnippetRepository snippets,
        IInlineReplacer replacer,
        ISecureInputDetector secureInput,
        ILogger<AutoExpandCoordinator> logger)
    {
        _hook = hook;
        _expansion = expansion;
        _snippets = snippets;
        _replacer = replacer;
        _secureInput = secureInput;
        _logger = logger;
    }

    /// <summary>Raised after a successful auto-expansion (for UI feedback).</summary>
    public event EventHandler<string>? Expanded;

    public void Start()
    {
        _hook.SnippetWordCompleted += OnWordCompleted;
        _hook.IsCompleteTrigger = word => _index.IsCompleteTrigger(word);
        _hook.IsKnownTrigger = word => _index.IsKnownTrigger(word);
        _refreshTimer = new System.Threading.Timer(_ => _ = RefreshIndexAsync(), null, TimeSpan.Zero, RefreshInterval);
    }

    public void Stop()
    {
        _hook.SnippetWordCompleted -= OnWordCompleted;
        _hook.IsCompleteTrigger = null;
        _hook.IsKnownTrigger = null;
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    private async Task RefreshIndexAsync()
    {
        try
        {
            var snippets = await _snippets.GetAllAsync();
            _index = new TriggerIndex(snippets.Where(s => s.IsEnabled).Select(s => s.Trigger));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to refresh the auto-expand trigger index.");
        }
    }

    private async void OnWordCompleted(object? sender, WordBoundary e)
    {
        // Drop overlapping events rather than queueing; a fast typist can finish several words while
        // one expansion is still injecting.
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            await GiveBackAsync(e);
            return;
        }

        try
        {
            if (_secureInput.IsFocusedFieldSecure())
            {
                await GiveBackAsync(e);
                return;
            }

            var result = await _expansion.TryExpandAsync(e.Word);
            if (!result.Matched || result.ExpandedText is null)
            {
                await GiveBackAsync(e);
                return;
            }

            // Delete the trigger and insert the rendered snippet, followed by the delimiter that closed
            // the word so the user's spacing is preserved. The hook swallowed that delimiter (and, for
            // an instant expansion, the trigger's last character), so it counted what actually reached
            // the document — we must not add anything to CharsToDelete.
            var replacement = result.ExpandedText + e.Boundary;
            if (await _replacer.ReplaceAsync(e.CharsToDelete, replacement, result.CursorOffset))
            {
                if (result.SnippetId is int id)
                {
                    await _expansion.RegisterUsageAsync(id);
                }

                Expanded?.Invoke(this, result.ExpandedText);
            }
            else
            {
                await GiveBackAsync(e);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-expand handling failed.");
            await GiveBackAsync(e);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    // The hook took the keystroke that completed the trigger so nothing could race our replacement.
    // If we end up not expanding, that keystroke is ours to return — otherwise the user silently loses
    // a character.
    private async Task GiveBackAsync(WordBoundary e)
    {
        if (e.SwallowedText.Length > 0)
        {
            await _replacer.TypeAsync(e.SwallowedText);
        }
    }

    public void Dispose() => Stop();
}
