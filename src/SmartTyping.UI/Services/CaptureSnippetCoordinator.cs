using System.Windows;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.UI.ViewModels;

namespace SmartTyping.UI.Services;

/// <summary>
/// Wires the capture hotkey (Ctrl+Shift+N) to "add snippet from selection": grabs the currently
/// selected text and opens the Add dialog pre-filled with it. Skips secure/password fields.
/// </summary>
public sealed class CaptureSnippetCoordinator : IDisposable
{
    private readonly IKeyboardHook _hook;
    private readonly ISelectionService _selection;
    private readonly ISecureInputDetector _secureInput;
    private readonly MainViewModel _mainViewModel;
    private readonly ILogger<CaptureSnippetCoordinator> _logger;

    private int _busy;

    public CaptureSnippetCoordinator(
        IKeyboardHook hook,
        ISelectionService selection,
        ISecureInputDetector secureInput,
        MainViewModel mainViewModel,
        ILogger<CaptureSnippetCoordinator> logger)
    {
        _hook = hook;
        _selection = selection;
        _secureInput = secureInput;
        _mainViewModel = mainViewModel;
        _logger = logger;
    }

    public void Start() => _hook.CaptureHotkeyPressed += OnHotkeyPressed;

    public void Stop() => _hook.CaptureHotkeyPressed -= OnHotkeyPressed;

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
        {
            return;
        }

        try
        {
            if (_secureInput.IsFocusedFieldSecure())
            {
                _logger.LogInformation("Capture-to-snippet skipped: focused field is secure.");
                return;
            }

            var selection = await _selection.CaptureSelectionAsync();
            if (string.IsNullOrWhiteSpace(selection))
            {
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var main = System.Windows.Application.Current.MainWindow;
                if (main is not null)
                {
                    main.Show();
                    main.WindowState = WindowState.Normal;
                    main.Activate();
                }

                _ = _mainViewModel.AddFromContentAsync(selection);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capture-to-snippet handling failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public void Dispose() => Stop();
}
