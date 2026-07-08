using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;
using SmartTyping.Application.Snippets;

namespace SmartTyping.UI.Services;

/// <summary>
/// Wires the global expansion hotkey to the snippet-expansion flow: read the trigger the user has
/// selected (e.g. <c>/phone</c>), expand it, and inject the result over the selection. Respects the
/// "snippet expansion enabled" setting. Runs on an explicit user action only (no auto-expand).
/// All failures are logged and swallowed.
/// </summary>
public sealed class SnippetExpansionCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly ISelectionService _selection;
    private readonly ITextInjector _injector;
    private readonly SnippetExpansionService _expansion;
    private readonly SettingsService _settings;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<SnippetExpansionCoordinator> _logger;

    private int _busy;

    public SnippetExpansionCoordinator(
        IKeyboardHook hook,
        ISelectionService selection,
        ITextInjector injector,
        SnippetExpansionService expansion,
        SettingsService settings,
        ISecureInputDetector secureInput,
        ILogger<SnippetExpansionCoordinator> logger)
    {
        _hook = hook;
        _selection = selection;
        _injector = injector;
        _expansion = expansion;
        _settings = settings;
        _secureInput = secureInput;
        _logger = logger;
    }

    /// <summary>Raised after a successful expansion so the UI can show brief feedback.</summary>
    public event EventHandler<string>? Expanded;

    public void Start() => _hook.ExpansionHotkeyPressed += OnHotkeyPressed;

    public void Stop() => _hook.ExpansionHotkeyPressed -= OnHotkeyPressed;

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            return;
        }

        try
        {
            if (!await _settings.IsSnippetExpansionEnabledAsync())
            {
                return;
            }

            // Never capture from or inject into a detected password field.
            if (_secureInput.IsFocusedFieldSecure())
            {
                _logger.LogInformation("Expansion skipped: focused field is a secure/password input.");
                return;
            }

            var trigger = (await _selection.CaptureSelectionAsync()).Trim();
            if (string.IsNullOrEmpty(trigger))
            {
                return;
            }

            var result = await _expansion.TryExpandAsync(trigger);
            if (!result.Matched || result.ExpandedText is null)
            {
                return;
            }

            if (await _injector.InjectAsync(result.ExpandedText, replaceSelection: true, result.CursorOffset))
            {
                Expanded?.Invoke(this, result.ExpandedText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expansion hotkey handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public void Dispose() => Stop();
}
