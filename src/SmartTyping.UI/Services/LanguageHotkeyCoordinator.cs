using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Application.Settings;

namespace SmartTyping.UI.Services;

/// <summary>
/// Wires the global conversion hotkey to the conversion flow: capture the current selection,
/// convert its layout, and inject the result back over the selection. Respects the
/// "language correction enabled" setting. All failures are logged and swallowed.
/// </summary>
public sealed class LanguageHotkeyCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly ISelectionService _selection;
    private readonly ITextInjector _injector;
    private readonly LanguageConversionService _conversion;
    private readonly SettingsService _settings;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<LanguageHotkeyCoordinator> _logger;

    private int _busy;

    public LanguageHotkeyCoordinator(
        IKeyboardHook hook,
        ISelectionService selection,
        ITextInjector injector,
        LanguageConversionService conversion,
        SettingsService settings,
        ISecureInputDetector secureInput,
        ILogger<LanguageHotkeyCoordinator> logger)
    {
        _hook = hook;
        _selection = selection;
        _injector = injector;
        _conversion = conversion;
        _settings = settings;
        _secureInput = secureInput;
        _logger = logger;
    }

    /// <summary>Raised after a successful conversion so the UI can show brief feedback.</summary>
    public event EventHandler<string>? Converted;

    public void Start()
    {
        _hook.ConversionHotkeyPressed += OnHotkeyPressed;
        _hook.Start();
    }

    public void Stop()
    {
        _hook.ConversionHotkeyPressed -= OnHotkeyPressed;
        _hook.Stop();
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Guard against overlapping runs (hotkey pressed repeatedly).
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            return;
        }

        try
        {
            if (!await _settings.IsLanguageCorrectionEnabledAsync())
            {
                return;
            }

            // Never capture from or inject into a detected password field.
            if (_secureInput.IsFocusedFieldSecure())
            {
                _logger.LogInformation("Conversion skipped: focused field is a secure/password input.");
                return;
            }

            var selection = await _selection.CaptureSelectionAsync();
            if (string.IsNullOrWhiteSpace(selection))
            {
                // Nothing selected — fall back to converting the last typed word (PRD FR-7):
                // auto-select the previous word, then convert/replace it.
                selection = await _selection.CaptureLastWordAsync();
            }

            if (string.IsNullOrWhiteSpace(selection))
            {
                return;
            }

            var converted = _conversion.ConvertAuto(selection);
            if (string.Equals(converted, selection, StringComparison.Ordinal))
            {
                return;
            }

            var injected = await _injector.InjectAsync(converted, replaceSelection: true);
            if (injected)
            {
                Converted?.Invoke(this, converted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion hotkey handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public void Dispose() => Stop();
}
