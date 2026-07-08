using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Application.Snippets;

namespace SmartTyping.UI.Services;

/// <summary>
/// Automatic snippet expansion as you type (opt-in): when the user finishes a word with a space or
/// tab and that word is a snippet trigger, the trigger is replaced in place with the rendered
/// snippet — no hotkey needed. Skips detected password fields. Failures are logged and swallowed.
/// </summary>
public sealed class AutoExpandCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly SnippetExpansionService _expansion;
    private readonly IInlineReplacer _replacer;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<AutoExpandCoordinator> _logger;

    private int _busy;

    public AutoExpandCoordinator(
        IKeyboardHook hook,
        SnippetExpansionService expansion,
        IInlineReplacer replacer,
        ISecureInputDetector secureInput,
        ILogger<AutoExpandCoordinator> logger)
    {
        _hook = hook;
        _expansion = expansion;
        _replacer = replacer;
        _secureInput = secureInput;
        _logger = logger;
    }

    /// <summary>Raised after a successful auto-expansion (for UI feedback).</summary>
    public event EventHandler<string>? Expanded;

    public void Start() => _hook.SnippetWordCompleted += OnWordCompleted;

    public void Stop() => _hook.SnippetWordCompleted -= OnWordCompleted;

    private async void OnWordCompleted(object? sender, WordBoundary e)
    {
        // Drop overlapping events rather than queueing; a fast typist can finish several words while
        // one expansion is still injecting.
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            return;
        }

        try
        {
            if (_secureInput.IsFocusedFieldSecure())
            {
                return;
            }

            var result = await _expansion.TryExpandAsync(e.Word);
            if (!result.Matched || result.ExpandedText is null)
            {
                return;
            }

            // Delete the trigger + its delimiter, then insert the rendered snippet followed by the
            // same delimiter so the user's spacing is preserved.
            var replacement = result.ExpandedText + e.Boundary;
            if (await _replacer.ReplaceAsync(e.Word.Length + e.Boundary.Length, replacement, result.CursorOffset))
            {
                Expanded?.Invoke(this, result.ExpandedText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-expand handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public void Dispose() => Stop();
}
