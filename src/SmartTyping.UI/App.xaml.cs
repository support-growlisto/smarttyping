using System.Linq;
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
    private CaptureSnippetCoordinator? _capture;
    private AiImproveCoordinator? _aiImprove;
    private AutoExpandCoordinator? _autoExpand;

    // Throttle the as-you-type suggestion balloon so it can't spam the tray.
    private int _lastSuggestionTick;

    // Guards the automatic layout-correct handler against overlapping in-flight corrections.
    private int _autoCorrectBusy;

    // The correction the undo hotkey would revert. The hook only lets undo fire while this is still
    // the last thing that happened, so it never goes stale in a harmful way.
    private Application.Language.LayoutCorrection? _lastCorrection;

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

        // UI Automation ships with WPF, which only this project references — so the keyboard hook's
        // "what is before the caret?" adapter is registered here rather than in Infrastructure.
        services.AddSingleton<Application.Abstractions.ICaretContext, Services.UiAutomationCaretContext>();

        services.AddSingleton<TrayIconService>();
        services.AddSingleton<LanguageHotkeyCoordinator>();
        services.AddSingleton<SnippetExpansionCoordinator>();
        services.AddSingleton<QuickPickerCoordinator>();
        services.AddSingleton<CaptureSnippetCoordinator>();
        services.AddSingleton<AiImproveCoordinator>();
        services.AddSingleton<AutoExpandCoordinator>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
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

        // Apply saved UI language (default Thai) and theme (default: follow system) before any window.
        try
        {
            var settings = _services.GetRequiredService<SettingsService>();
            Localization.LocalizationManager.Instance.SetLanguage(settings.GetLanguageAsync().GetAwaiter().GetResult());
            Themes.ThemeManager.Apply(settings.GetThemeAsync().GetAwaiter().GetResult());
            var hook = _services.GetRequiredService<IKeyboardHook>();
            hook.UpdateBindings(settings.GetHotkeysAsync().GetAwaiter().GetResult());
            hook.SuggestionsEnabled = settings.IsAutoCorrectSuggestEnabledAsync().GetAwaiter().GetResult();
            hook.AutoApplySuggestions = settings.IsAutoCorrectAutoApplyEnabledAsync().GetAwaiter().GetResult();
            hook.AutoExpandEnabled = settings.IsAutoExpandEnabledAsync().GetAwaiter().GetResult();
            hook.Blocklist = settings.GetBlockedAppsAsync().GetAwaiter().GetResult();

            // Correcting mid-word only makes sense if we can then switch the user to Thai; otherwise
            // the rest of the word would keep coming out latin. Fall back to the space-boundary fix.
            hook.ImmediateLayoutCorrect = _services.GetRequiredService<IKeyboardLayoutSwitcher>().IsThaiAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load UI language/theme/hotkeys; using defaults.");
            Themes.ThemeManager.Apply(Themes.ThemeManager.System);
        }

        var mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        // Tray icon.
        _tray = _services.GetRequiredService<TrayIconService>();
        _tray.Initialize(mainWindow, ExitApplication);
        try
        {
            _tray.NotificationsEnabled = _services.GetRequiredService<SettingsService>()
                .IsNotificationsEnabledAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read the notifications setting; leaving them enabled.");
        }

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

        _capture = _services.GetRequiredService<CaptureSnippetCoordinator>();
        _capture.Start();

        // AI improve (opt-in; does nothing unless the user set an API key).
        _aiImprove = _services.GetRequiredService<AiImproveCoordinator>();
        _aiImprove.Improved += (_, text) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_AiImproved"], Preview(text)));
        _aiImprove.Working += (_, _) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_AiWorking_Title"],
                Localization.LocalizationManager.Instance["Tray_AiWorking"]));
        _aiImprove.NotConfigured += (_, _) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_AiNotConfigured_Title"],
                Localization.LocalizationManager.Instance["Tray_AiNotConfigured"]));
        _aiImprove.Start();

        // Automatic snippet expansion as you type (opt-in).
        _autoExpand = _services.GetRequiredService<AutoExpandCoordinator>();
        _autoExpand.Expanded += (_, text) =>
            Dispatcher.Invoke(() => _tray?.ShowBalloon(Localization.LocalizationManager.Instance["Tray_Expanded"], Preview(text)));
        _autoExpand.Start();

        // As-you-type layout: a non-destructive hint, or (opt-in) an automatic in-place fix.
        var keyboardHook = _services.GetRequiredService<IKeyboardHook>();
        keyboardHook.LayoutSuggestionRaised += OnLayoutSuggestionRaised;
        keyboardHook.LayoutAutoCorrectRequested += OnLayoutAutoCorrectRequested;
        keyboardHook.UndoCorrectionRequested += OnUndoCorrectionRequested;

        // Surface the window when a second launch signals us.
        _singleInstance.StartListening(() => Dispatcher.Invoke(ShowMainWindow));

        // Launched by Windows at sign-in: live in the tray, silently. Everything above is already
        // running — the hooks, the corrector, the expander — so there is nothing a window would add
        // except getting in the way of whatever the user actually signed in to do.
        var background = e.Args.Any(a =>
            string.Equals(a, IStartupService.BackgroundFlag, StringComparison.OrdinalIgnoreCase));

        if (!background)
        {
            mainWindow.Show();
            _ = ShowOnboardingIfFirstRunAsync();
        }

        // An entry written by an older version launches without the flag, so it would keep opening a
        // window at every sign-in until the user toggles the setting. Repair it in place.
        try
        {
            _services.GetRequiredService<IStartupService>().RefreshIfEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Could not refresh the start-with-Windows entry.");
        }

        _ = CheckForUpdatesOnStartupAsync();
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var settings = _services!.GetRequiredService<SettingsService>();
            if (!await settings.IsUpdateCheckEnabledAsync())
            {
                return;
            }

            var info = await _services.GetRequiredService<IUpdateService>().CheckForUpdateAsync();
            if (info is not null)
            {
                var loc = Localization.LocalizationManager.Instance;
                Dispatcher.Invoke(() => _tray?.ShowBalloon(loc["Update_Title"], loc.Format("Update_Available", info.Version)));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Startup update check failed.");
        }
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
        _capture?.Stop();
        _aiImprove?.Stop();
        _autoExpand?.Stop();
        _tray?.Dispose();
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        _expansion?.Dispose();
        _picker?.Dispose();
        _capture?.Dispose();
        _aiImprove?.Dispose();
        _autoExpand?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }

    private void OnLayoutSuggestionRaised(object? sender, Application.Language.LayoutSuggestion suggestion)
    {
        // Throttle: at most one hint per ~4 seconds, regardless of typing speed.
        var now = Environment.TickCount;
        if (unchecked(now - _lastSuggestionTick) < 4000)
        {
            return;
        }
        _lastSuggestionTick = now;

        var loc = Localization.LocalizationManager.Instance;
        Dispatcher.Invoke(() => _tray?.ShowBalloon(loc["Tray_Suggestion"],
            loc.Format("Tray_SuggestionBody", suggestion.Original, suggestion.Suggestion)));
    }

    private async void OnLayoutAutoCorrectRequested(object? sender, Application.Language.LayoutCorrection correction)
    {
        var replacer = _services!.GetRequiredService<IInlineReplacer>();

        // Drop overlapping requests so two in-flight corrections can't interleave their input.
        if (Interlocked.Exchange(ref _autoCorrectBusy, 1) == 1)
        {
            await GiveBackAsync(replacer, correction);
            return;
        }

        try
        {
            // Delete only what actually reached the document — the hook counted it, and it swallowed
            // the keystroke that triggered us. Type the fix, then the delimiter that closed the word.
            var ok = await replacer.ReplaceAsync(
                correction.CharsToDelete,
                correction.Suggestion + correction.Boundary);

            if (!ok)
            {
                await GiveBackAsync(replacer, correction);
                return;
            }

            // The user meant the other language, so switch the input language — otherwise the rest of
            // the word keeps coming out wrong and they'd have to fix it again.
            _services.GetRequiredService<IKeyboardLayoutSwitcher>().SwitchForeground(correction.ToThai);

            // Remember it so the undo hotkey can put it back verbatim.
            _lastCorrection = correction;

            var loc = Localization.LocalizationManager.Instance;
            Dispatcher.Invoke(() => _tray?.ShowBalloon(loc["Tray_AutoFixed"],
                loc.Format("Tray_AutoFixedBody", correction.Original, correction.Suggestion)));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Automatic layout correction failed.");
            await GiveBackAsync(replacer, correction);
        }
        finally
        {
            Interlocked.Exchange(ref _autoCorrectBusy, 0);
        }
    }

    // The hook swallowed the keystroke that triggered the correction, so that our backspaces could not
    // race it into the document. If the correction doesn't happen, that keystroke is ours to return.
    private static async Task GiveBackAsync(IInlineReplacer replacer, Application.Language.LayoutCorrection correction)
    {
        if (correction.SwallowedText.Length > 0)
        {
            await replacer.TypeAsync(correction.SwallowedText);
        }
    }

    /// <summary>
    /// Puts back exactly what the user typed before the last automatic correction, restores their
    /// keyboard layout, and teaches the lexicon that the original text was a real word — so the
    /// decider's veto stops correcting it from now on.
    /// </summary>
    private async void OnUndoCorrectionRequested(object? sender, EventArgs e)
    {
        var correction = _lastCorrection;
        if (correction is null)
        {
            return;
        }

        _lastCorrection = null;

        try
        {
            var replacer = _services!.GetRequiredService<IInlineReplacer>();
            var restored = await replacer.ReplaceAsync(
                correction.Suggestion.Length + correction.Boundary.Length,
                correction.Original + correction.Boundary);

            if (!restored)
            {
                return;
            }

            // We had switched the layout to finish the word in the other language; switch it back.
            _services.GetRequiredService<IKeyboardLayoutSwitcher>().SwitchForeground(!correction.ToThai);

            // Original is the text that was on screen: latin when we converted to Thai, Thai when we
            // converted back to English. Learn it as a word of the language it was displayed in.
            _services.GetRequiredService<Application.Language.ILexicon>()
                .Learn(correction.Original, isThai: !correction.ToThai);

            var loc = Localization.LocalizationManager.Instance;
            Dispatcher.Invoke(() => _tray?.ShowBalloon(loc["Tray_Undone"],
                loc.Format("Tray_UndoneBody", correction.Original)));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Undoing the last layout correction failed.");
        }
    }

    private static string Preview(string text)
    {
        var oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return oneLine.Length > 60 ? oneLine[..60] + "…" : oneLine;
    }
}
