using System.Windows;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;
using SmartTyping.Application.Snippets;
using SmartTyping.UI.ViewModels;
using SmartTyping.UI.Views;

namespace SmartTyping.UI.Services;

/// <summary>
/// Wires the quick-picker hotkey (Ctrl+Shift+Space) to a searchable snippet list. Remembers the
/// foreground app, shows the picker, then restores focus and inserts the chosen snippet at the caret.
/// Explicit user action only. Failures are logged and swallowed.
/// </summary>
public sealed class QuickPickerCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly IForegroundWindowService _foreground;
    private readonly ISnippetRepository _snippets;
    private readonly SnippetExpansionService _expansion;
    private readonly ITextInjector _injector;
    private readonly SettingsService _settings;
    private readonly ISecureInputDetector _secureInput;
    private readonly ILogger<QuickPickerCoordinator> _logger;

    private int _busy;

    public QuickPickerCoordinator(
        IKeyboardHook hook,
        IForegroundWindowService foreground,
        ISnippetRepository snippets,
        SnippetExpansionService expansion,
        ITextInjector injector,
        SettingsService settings,
        ISecureInputDetector secureInput,
        ILogger<QuickPickerCoordinator> logger)
    {
        _hook = hook;
        _foreground = foreground;
        _snippets = snippets;
        _expansion = expansion;
        _injector = injector;
        _settings = settings;
        _secureInput = secureInput;
        _logger = logger;
    }

    public event EventHandler<string>? Inserted;

    public void Start() => _hook.PickerHotkeyPressed += OnHotkeyPressed;

    public void Stop() => _hook.PickerHotkeyPressed -= OnHotkeyPressed;

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

            // Remember the app the user was typing in before our picker steals focus.
            var target = _foreground.CaptureForeground();

            var trigger = System.Windows.Application.Current.Dispatcher.Invoke(ShowPicker);
            if (string.IsNullOrEmpty(trigger))
            {
                return;
            }

            // Return focus to the original app, then insert at the caret.
            _foreground.Restore(target);
            await Task.Delay(120);

            if (_secureInput.IsFocusedFieldSecure())
            {
                _logger.LogInformation("Quick-picker insert skipped: focused field is secure.");
                return;
            }

            var result = await _expansion.TryExpandAsync(trigger);
            if (result.Matched && result.ExpandedText is not null &&
                await _injector.InjectAsync(result.ExpandedText, replaceSelection: false, result.CursorOffset))
            {
                if (result.SnippetId is int id)
                {
                    await _expansion.RegisterUsageAsync(id);
                }

                Inserted?.Invoke(this, result.ExpandedText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick-picker handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    /// <summary>Shows the picker modally on the UI thread and returns the chosen trigger (or null).</summary>
    private string? ShowPicker()
    {
        var window = new QuickPickerWindow(new QuickPickerViewModel(_snippets));
        return window.ShowDialog() == true ? window.SelectedTrigger : null;
    }

    public void Dispose() => Stop();
}
