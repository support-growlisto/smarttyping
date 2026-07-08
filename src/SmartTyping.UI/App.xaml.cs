using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTyping.Application;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;
using SmartTyping.Infrastructure;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.UI.Services;
using SmartTyping.UI.ViewModels;
using SmartTyping.UI.Views;

namespace SmartTyping.UI;

/// <summary>
/// Application entry point and composition root. All dependency wiring happens here; no other
/// layer constructs infrastructure directly.
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;
    private ILogger<App>? _logger;
    private SingleInstanceGuard? _singleInstance;
    private TrayIconService? _tray;
    private LanguageHotkeyCoordinator? _hotkey;
    private SnippetExpansionCoordinator? _expansion;
    private QuickPickerCoordinator? _picker;

    /// <summary>True once the user has chosen to exit, so the main window stops hiding-to-tray.</summary>
    public static bool IsShuttingDown { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance: if we're not the first, ask the running instance to surface and exit.
        _singleInstance = new SingleInstanceGuard("SmartTyping");
        if (!_singleInstance.IsFirstInstance)
        {
            _singleInstance.SignalExistingInstance();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(0);
            return;
        }

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure();

        // UI-layer services.
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IPlaceholderPrompt, PlaceholderPromptService>();
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<LanguageHotkeyCoordinator>();
        services.AddSingleton<SnippetExpansionCoordinator>();
        services.AddSingleton<QuickPickerCoordinator>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _services = services.BuildServiceProvider();
        _logger = _services.GetRequiredService<ILogger<App>>();

        // Global crash handling: log everything, keep the app alive for recoverable UI faults.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Initialize the database (schema + seed) before anything queries it.
        try
        {
            _services.GetRequiredService<DatabaseInitializer>().Initialize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            System.Windows.MessageBox.Show("Failed to initialize the database. See logs for details.", "SmartTyping",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Apply the saved UI language (default Thai) before any window is created.
        try
        {
            var language = _services.GetRequiredService<SettingsService>().GetLanguageAsync().GetAwaiter().GetResult();
            Localization.LocalizationManager.Instance.SetLanguage(language);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load UI language; using default.");
        }

        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        // Tray icon.
        _tray = _services.GetRequiredService<TrayIconService>();
        _tray.Initialize(mainWindow, ExitApplication);

        // Global hotkeys (must start on the UI thread — it owns the message loop that hooks require).
        _hotkey = _services.GetRequiredService<LanguageHotkeyCoordinator>();
        _hotkey.Converted += (_, text) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_Converted"], Preview(text)));
        _hotkey.Start();

        _expansion = _services.GetRequiredService<SnippetExpansionCoordinator>();
        _expansion.Expanded += (_, text) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_Expanded"], Preview(text)));
        _expansion.Start();

        _picker = _services.GetRequiredService<QuickPickerCoordinator>();
        _picker.Inserted += (_, text) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_Expanded"], Preview(text)));
        _picker.Start();

        // Surface the window when a second launch signals us.
        _singleInstance.StartListening(() => Dispatcher.Invoke(ShowMainWindow));

        mainWindow.Show();

        _ = ShowOnboardingIfFirstRunAsync();
    }

    private async Task ShowOnboardingIfFirstRunAsync()
    {
        try
        {
            var settings = _services!.GetRequiredService<SettingsService>();
            if (await settings.IsOnboardingCompletedAsync())
            {
                return;
            }

            var onboarding = new OnboardingWindow { Owner = MainWindow };
            onboarding.ShowDialog();
            await settings.SetOnboardingCompletedAsync(true);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Onboarding failed to display.");
        }
    }

    private void ShowMainWindow()
    {
        if (MainWindow is null)
        {
            return;
        }

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled UI exception.");
        // Recoverable: keep the app running rather than crashing the whole process on one bad action.
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogCritical(ex, "Unhandled non-UI exception (app terminating: {IsTerminating}).", e.IsTerminating);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }

    private void ExitApplication()
    {
        IsShuttingDown = true;
        _hotkey?.Stop();
        _expansion?.Stop();
        _picker?.Stop();
        _tray?.Dispose();
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        _expansion?.Dispose();
        _picker?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }

    private static string Preview(string text)
    {
        var oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return oneLine.Length > 60 ? oneLine[..60] + "…" : oneLine;
    }
}
