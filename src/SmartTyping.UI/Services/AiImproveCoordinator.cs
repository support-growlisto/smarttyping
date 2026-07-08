using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.UI.Services;

/// <summary>
/// Wires the AI-improve hotkey (Ctrl+Shift+I) to: capture the selection, send it to the AI service
/// (Gemini), and replace it with the improved text. Opt-in — does nothing if no API key is set.
/// Skips detected password fields. Failures are logged and swallowed.
/// </summary>
public sealed class AiImproveCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly ISelectionService _selection;
    private readonly ITextInjector _injector;
    private readonly IAiService _ai;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<AiImproveCoordinator> _logger;

    private int _busy;

    public AiImproveCoordinator(
        IKeyboardHook hook,
        ISelectionService selection,
        ITextInjector injector,
        IAiService ai,
        ISecureInputDetector secureInput,
        ILogger<AiImproveCoordinator> logger)
    {
        _hook = hook;
        _selection = selection;
        _injector = injector;
        _ai = ai;
        _secureInput = secureInput;
        _logger = logger;
    }

    /// <summary>Raised after a successful improvement (for UI feedback).</summary>
    public event EventHandler<string>? Improved;

    /// <summary>Raised when the hotkey is pressed but no API key is configured.</summary>
    public event EventHandler? NotConfigured;

    /// <summary>Raised while the AI request is in flight (for a "thinking…" hint).</summary>
    public event EventHandler? Working;

    public void Start() => _hook.AiImproveHotkeyPressed += OnHotkeyPressed;

    public void Stop() => _hook.AiImproveHotkeyPressed -= OnHotkeyPressed;

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            return;
        }

        try
        {
            if (!await _ai.IsConfiguredAsync())
            {
                NotConfigured?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_secureInput.IsFocusedFieldSecure())
            {
                _logger.LogInformation("AI improve skipped: focused field is secure.");
                return;
            }

            var selection = await _selection.CaptureSelectionAsync();
            if (string.IsNullOrWhiteSpace(selection))
            {
                return;
            }

            Working?.Invoke(this, EventArgs.Empty);
            var improved = await _ai.ImproveAsync(selection);
            if (string.IsNullOrWhiteSpace(improved))
            {
                return;
            }

            if (await _injector.InjectAsync(improved, replaceSelection: true))
            {
                Improved?.Invoke(this, improved);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI improve handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public void Dispose() => Stop();
}
