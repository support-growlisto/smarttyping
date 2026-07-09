using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;
using SmartTyping.Domain.Enums;
using SmartTyping.Domain.ValueObjects;
using SmartTyping.UI.Localization;
using SmartTyping.UI.Mvvm;
using SmartTyping.UI.Services;
using SmartTyping.UI.Themes;

namespace SmartTyping.UI.ViewModels;

/// <summary>A selectable UI language.</summary>
public sealed record LanguageOption(string Code, string DisplayName);

/// <summary>A selectable UI theme (system/light/dark).</summary>
public sealed record ThemeOption(string Code, string DisplayName);

/// <summary>A rebindable hotkey row shown in Settings.</summary>
public sealed class HotkeyRowViewModel : ObservableObject
{
    private string _combo = string.Empty;

    public HotkeyRowViewModel(HotkeyAction action, string label, ICommand changeCommand)
    {
        Action = action;
        Label = label;
        ChangeCommand = changeCommand;
    }

    public HotkeyAction Action { get; }
    public string Label { get; }
    public ICommand ChangeCommand { get; }

    public string Combo
    {
        get => _combo;
        set => SetProperty(ref _combo, value);
    }
}

/// <summary>View model for the settings view. Persists each toggle immediately.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly IStartupService _startup;
    private readonly IKeyboardHook _hook;
    private readonly IUpdateService _updates;
    private readonly IDialogService _dialogs;
    private readonly TrayIconService _tray;
    private readonly Dictionary<HotkeyAction, Hotkey> _hotkeys = new(SettingsService.DefaultHotkeys);
    private bool _loading;

    private bool _snippetExpansionEnabled = true;
    private bool _languageCorrectionEnabled = true;
    private bool _autoCorrectSuggestEnabled;
    private bool _autoCorrectAutoApply;
    private bool _autoExpandEnabled;
    private bool _notificationsEnabled = true;
    private string _aiApiKey = string.Empty;
    private bool _startWithWindows;
    private bool _checkForUpdates;
    private string _updateStatus = string.Empty;
    private LanguageOption _selectedLanguage;
    private ThemeOption _selectedTheme;

    public SettingsViewModel(SettingsService settings, IStartupService startup, IKeyboardHook hook, IUpdateService updates, IDialogService dialogs, TrayIconService tray)
    {
        _settings = settings;
        _startup = startup;
        _hook = hook;
        _updates = updates;
        _dialogs = dialogs;
        _tray = tray;
        _selectedLanguage = Languages[0];
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesNowAsync);

        var loc = LocalizationManager.Instance;
        Themes = new[]
        {
            new ThemeOption(ThemeManager.System, loc["Theme_System"]),
            new ThemeOption(ThemeManager.Light, loc["Theme_Light"]),
            new ThemeOption(ThemeManager.Dark, loc["Theme_Dark"])
        };
        _selectedTheme = Themes[0];

        HotkeyRows = new ObservableCollection<HotkeyRowViewModel>
        {
            NewRow(HotkeyAction.Convert, loc["Settings_ConvertLayout"]),
            NewRow(HotkeyAction.Expand, loc["Settings_ExpandSnippet"]),
            NewRow(HotkeyAction.Picker, loc["Settings_Picker"]),
            NewRow(HotkeyAction.Capture, loc["Settings_Capture"]),
            NewRow(HotkeyAction.AiImprove, loc["Settings_AiImprove"])
        };
        RefreshHotkeyRows();
    }

    public ObservableCollection<HotkeyRowViewModel> HotkeyRows { get; }

    private HotkeyRowViewModel NewRow(HotkeyAction action, string label)
    {
        HotkeyRowViewModel? row = null;
        row = new HotkeyRowViewModel(action, label, new RelayCommand(() => ChangeHotkey(row!)));
        return row;
    }

    public IReadOnlyList<LanguageOption> Languages { get; } = new[]
    {
        new LanguageOption(LocalizationManager.Thai, "ไทย"),
        new LanguageOption(LocalizationManager.English, "English")
    };

    public IReadOnlyList<ThemeOption> Themes { get; }

    public ThemeOption SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value) && value is not null)
            {
                ThemeManager.Apply(value.Code);
                if (!_loading)
                {
                    _ = _settings.SetThemeAsync(value.Code);
                }
            }
        }
    }

    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null)
            {
                LocalizationManager.Instance.SetLanguage(value.Code);
                if (!_loading)
                {
                    _ = _settings.SetLanguageAsync(value.Code);
                }
            }
        }
    }

    private void ChangeHotkey(HotkeyRowViewModel row)
    {
        var captured = _dialogs.RecordHotkey();
        if (captured is null)
        {
            return;
        }

        var hotkey = captured.Value;
        if (_hotkeys.Any(kv => kv.Key != row.Action && kv.Value == hotkey))
        {
            var loc = LocalizationManager.Instance;
            _dialogs.ShowMessage(loc["Hotkey_Duplicate"], loc["Settings_Hotkeys"]);
            return;
        }

        _hotkeys[row.Action] = hotkey;
        row.Combo = hotkey.ToStorageString();
        _hook.UpdateBindings(new Dictionary<HotkeyAction, Hotkey>(_hotkeys));
        _ = _settings.SetHotkeyAsync(row.Action, hotkey);
    }

    private void RefreshHotkeyRows()
    {
        foreach (var row in HotkeyRows)
        {
            row.Combo = _hotkeys[row.Action].ToStorageString();
        }
    }

    public bool SnippetExpansionEnabled
    {
        get => _snippetExpansionEnabled;
        set
        {
            if (SetProperty(ref _snippetExpansionEnabled, value) && !_loading)
            {
                _ = _settings.SetSnippetExpansionEnabledAsync(value);
            }
        }
    }

    public bool LanguageCorrectionEnabled
    {
        get => _languageCorrectionEnabled;
        set
        {
            if (SetProperty(ref _languageCorrectionEnabled, value) && !_loading)
            {
                _ = _settings.SetLanguageCorrectionEnabledAsync(value);
            }
        }
    }

    public bool AutoCorrectSuggestEnabled
    {
        get => _autoCorrectSuggestEnabled;
        set
        {
            if (SetProperty(ref _autoCorrectSuggestEnabled, value))
            {
                // Live-toggle the hook so the change takes effect without a restart.
                _hook.SuggestionsEnabled = value;
                if (!_loading)
                {
                    _ = _settings.SetAutoCorrectSuggestEnabledAsync(value);
                }
                OnPropertyChanged(nameof(CanAutoApply));
            }
        }
    }

    /// <summary>The automatic-fix sub-option only makes sense while suggestions are enabled.</summary>
    public bool CanAutoApply => _autoCorrectSuggestEnabled;

    public bool AutoCorrectAutoApply
    {
        get => _autoCorrectAutoApply;
        set
        {
            if (SetProperty(ref _autoCorrectAutoApply, value))
            {
                _hook.AutoApplySuggestions = value;
                if (!_loading)
                {
                    _ = _settings.SetAutoCorrectAutoApplyEnabledAsync(value);
                }
            }
        }
    }

    public bool AutoExpandEnabled
    {
        get => _autoExpandEnabled;
        set
        {
            if (SetProperty(ref _autoExpandEnabled, value))
            {
                _hook.AutoExpandEnabled = value;
                if (!_loading)
                {
                    _ = _settings.SetAutoExpandEnabledAsync(value);
                }
            }
        }
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (SetProperty(ref _notificationsEnabled, value))
            {
                // Live-apply so the next expansion is already silent.
                _tray.NotificationsEnabled = value;
                if (!_loading)
                {
                    _ = _settings.SetNotificationsEnabledAsync(value);
                }
            }
        }
    }

    public string AiApiKey
    {
        get => _aiApiKey;
        set
        {
            if (SetProperty(ref _aiApiKey, value) && !_loading)
            {
                _ = _settings.SetAiApiKeyAsync(value?.Trim() ?? string.Empty);
            }
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value) && !_loading)
            {
                if (value)
                {
                    _startup.Enable();
                }
                else
                {
                    _startup.Disable();
                }

                _ = _settings.SetStartWithWindowsAsync(value);
            }
        }
    }

    public bool CheckForUpdates
    {
        get => _checkForUpdates;
        set
        {
            if (SetProperty(ref _checkForUpdates, value) && !_loading)
            {
                _ = _settings.SetUpdateCheckEnabledAsync(value);
            }
        }
    }

    public string UpdateStatus
    {
        get => _updateStatus;
        private set => SetProperty(ref _updateStatus, value);
    }

    public ICommand CheckForUpdatesCommand { get; }

    private async Task CheckForUpdatesNowAsync()
    {
        var loc = LocalizationManager.Instance;
        UpdateStatus = loc["Update_Checking"];
        var info = await _updates.CheckForUpdateAsync();
        if (info is null)
        {
            UpdateStatus = loc["Update_UpToDate"];
            return;
        }

        UpdateStatus = loc.Format("Update_Available", info.Version);
        if (_dialogs.Confirm(loc.Format("Update_AvailablePrompt", info.Version), loc["Update_Title"]))
        {
            DiagnosticsLauncher.Open(info.DownloadUrl);
        }
    }

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            SnippetExpansionEnabled = await _settings.IsSnippetExpansionEnabledAsync();
            LanguageCorrectionEnabled = await _settings.IsLanguageCorrectionEnabledAsync();
            AutoCorrectSuggestEnabled = await _settings.IsAutoCorrectSuggestEnabledAsync();
            AutoCorrectAutoApply = await _settings.IsAutoCorrectAutoApplyEnabledAsync();
            AutoExpandEnabled = await _settings.IsAutoExpandEnabledAsync();
            NotificationsEnabled = await _settings.IsNotificationsEnabledAsync();
            AiApiKey = await _settings.GetAiApiKeyAsync();
            // The registry is the source of truth for auto-start.
            StartWithWindows = _startup.IsEnabled();
            CheckForUpdates = await _settings.IsUpdateCheckEnabledAsync();

            var languageCode = await _settings.GetLanguageAsync();
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == languageCode) ?? Languages[0];

            var themeCode = await _settings.GetThemeAsync();
            SelectedTheme = Themes.FirstOrDefault(t => t.Code == themeCode) ?? Themes[0];

            var hotkeys = await _settings.GetHotkeysAsync();
            foreach (var (action, hotkey) in hotkeys)
            {
                _hotkeys[action] = hotkey;
            }

            RefreshHotkeyRows();
        }
        finally
        {
            _loading = false;
        }
    }
}
