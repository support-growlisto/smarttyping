using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Settings;
using SmartTyping.UI.Localization;
using SmartTyping.UI.Mvvm;
using SmartTyping.UI.Themes;

namespace SmartTyping.UI.ViewModels;

/// <summary>A selectable UI language.</summary>
public sealed record LanguageOption(string Code, string DisplayName);

/// <summary>A selectable UI theme (system/light/dark).</summary>
public sealed record ThemeOption(string Code, string DisplayName);

/// <summary>View model for the settings view. Persists each toggle immediately.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly IStartupService _startup;
    private bool _loading;

    private bool _snippetExpansionEnabled = true;
    private bool _languageCorrectionEnabled = true;
    private bool _startWithWindows;
    private LanguageOption _selectedLanguage;
    private ThemeOption _selectedTheme;

    public SettingsViewModel(SettingsService settings, IStartupService startup)
    {
        _settings = settings;
        _startup = startup;
        _selectedLanguage = Languages[0];

        var loc = LocalizationManager.Instance;
        Themes = new[]
        {
            new ThemeOption(ThemeManager.System, loc["Theme_System"]),
            new ThemeOption(ThemeManager.Light, loc["Theme_Light"]),
            new ThemeOption(ThemeManager.Dark, loc["Theme_Dark"])
        };
        _selectedTheme = Themes[0];
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

    /// <summary>The fixed conversion hotkey (configurable in a later version).</summary>
    public string ConversionHotkeyText => "Ctrl + Shift + L";

    /// <summary>The fixed snippet-expansion hotkey (configurable in a later version).</summary>
    public string ExpansionHotkeyText => "Ctrl + Shift + E";

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

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            SnippetExpansionEnabled = await _settings.IsSnippetExpansionEnabledAsync();
            LanguageCorrectionEnabled = await _settings.IsLanguageCorrectionEnabledAsync();
            // The registry is the source of truth for auto-start.
            StartWithWindows = _startup.IsEnabled();

            var languageCode = await _settings.GetLanguageAsync();
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == languageCode) ?? Languages[0];

            var themeCode = await _settings.GetThemeAsync();
            SelectedTheme = Themes.FirstOrDefault(t => t.Code == themeCode) ?? Themes[0];
        }
        finally
        {
            _loading = false;
        }
    }
}
