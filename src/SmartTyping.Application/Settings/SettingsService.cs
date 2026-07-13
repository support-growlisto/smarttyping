using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Enums;
using SmartTyping.Domain.ValueObjects;

namespace SmartTyping.Application.Settings;

/// <summary>
/// Typed access to application settings backed by <see cref="ISettingsRepository"/>.
/// Provides the two MVP feature toggles and persists changes immediately.
/// </summary>
public sealed class SettingsService
{
    private readonly ISettingsRepository _repository;

    public SettingsService(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> IsSnippetExpansionEnabledAsync() =>
        GetBoolAsync(SettingKeys.SnippetExpansionEnabled, defaultValue: true);

    public Task SetSnippetExpansionEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.SnippetExpansionEnabled, enabled);

    public Task<bool> IsLanguageCorrectionEnabledAsync() =>
        GetBoolAsync(SettingKeys.LanguageCorrectionEnabled, defaultValue: true);

    public Task SetLanguageCorrectionEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.LanguageCorrectionEnabled, enabled);

    public Task<bool> IsOnboardingCompletedAsync() =>
        GetBoolAsync(SettingKeys.OnboardingCompleted, defaultValue: false);

    public Task SetOnboardingCompletedAsync(bool completed) =>
        SetBoolAsync(SettingKeys.OnboardingCompleted, completed);

    public Task SetStartWithWindowsAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.StartWithWindows, enabled);

    /// <summary>Whether to check for updates (the app's only network feature). Off by default.</summary>
    public Task<bool> IsUpdateCheckEnabledAsync() =>
        GetBoolAsync(SettingKeys.CheckForUpdates, defaultValue: false);

    public Task SetUpdateCheckEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.CheckForUpdates, enabled);

    /// <summary>As-you-type layout suggestions (non-destructive hints). Off by default.</summary>
    public Task<bool> IsAutoCorrectSuggestEnabledAsync() =>
        GetBoolAsync(SettingKeys.AutoCorrectSuggest, defaultValue: false);

    public Task SetAutoCorrectSuggestEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.AutoCorrectSuggest, enabled);

    /// <summary>When as-you-type is on, correct automatically (true) vs only suggest (false, default).</summary>
    public Task<bool> IsAutoCorrectAutoApplyEnabledAsync() =>
        GetBoolAsync(SettingKeys.AutoCorrectAuto, defaultValue: false);

    public Task SetAutoCorrectAutoApplyEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.AutoCorrectAuto, enabled);

    /// <summary>Automatic snippet expansion as you type (no hotkey). Off by default.</summary>
    public Task<bool> IsAutoExpandEnabledAsync() =>
        GetBoolAsync(SettingKeys.AutoExpandSnippets, defaultValue: false);

    public Task SetAutoExpandEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.AutoExpandSnippets, enabled);

    /// <summary>
    /// The personal dictionary — learning the words you type that no dictionary knows. <b>Off by
    /// default</b>: it is the only feature that writes what you type to disk, so it waits to be asked.
    /// </summary>
    public Task<bool> IsPersonalDictionaryEnabledAsync() =>
        GetBoolAsync(SettingKeys.PersonalDictionary, defaultValue: false);

    public Task SetPersonalDictionaryEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.PersonalDictionary, enabled);

    /// <summary>Tray balloon notifications (converted / expanded / AI). On by default.</summary>
    public Task<bool> IsNotificationsEnabledAsync() =>
        GetBoolAsync(SettingKeys.ShowNotifications, defaultValue: true);

    public Task SetNotificationsEnabledAsync(bool enabled) =>
        SetBoolAsync(SettingKeys.ShowNotifications, enabled);

    /// <summary>Apps where the automatic features must stay silent. Falls back to the defaults.</summary>
    public async Task<AppBlocklist> GetBlockedAppsAsync() =>
        AppBlocklist.Parse(await _repository.GetAsync(SettingKeys.BlockedApps));

    public Task SetBlockedAppsAsync(AppBlocklist blocklist) =>
        _repository.SetAsync(SettingKeys.BlockedApps, blocklist.ToString());

    /// <summary>The AI provider id (e.g. "gemini"). Defaults to Gemini.</summary>
    public async Task<string> GetAiProviderAsync()
    {
        var value = await _repository.GetAsync(SettingKeys.AiProvider);
        return string.IsNullOrWhiteSpace(value) ? "gemini" : value.Trim();
    }

    public Task SetAiProviderAsync(string provider) =>
        _repository.SetAsync(SettingKeys.AiProvider, provider);

    /// <summary>The user's AI API key (empty when unset). Enables the AI feature when present.</summary>
    public async Task<string> GetAiApiKeyAsync() =>
        await _repository.GetAsync(SettingKeys.AiApiKey) ?? string.Empty;

    public Task SetAiApiKeyAsync(string key) =>
        _repository.SetAsync(SettingKeys.AiApiKey, key);

    /// <summary>The UI language code (e.g. "th", "en"). Defaults to Thai.</summary>
    public async Task<string> GetLanguageAsync()
    {
        var value = await _repository.GetAsync(SettingKeys.Language);
        return string.IsNullOrWhiteSpace(value) ? "th" : value.Trim();
    }

    public Task SetLanguageAsync(string language) =>
        _repository.SetAsync(SettingKeys.Language, language);

    /// <summary>The UI theme: "system" (default), "light", or "dark".</summary>
    public async Task<string> GetThemeAsync()
    {
        var value = await _repository.GetAsync(SettingKeys.Theme);
        return string.IsNullOrWhiteSpace(value) ? "system" : value.Trim();
    }

    public Task SetThemeAsync(string theme) =>
        _repository.SetAsync(SettingKeys.Theme, theme);

    /// <summary>The default hotkeys used when none is saved (Ctrl+Shift+L / E / Space / N).</summary>
    public static IReadOnlyDictionary<HotkeyAction, Hotkey> DefaultHotkeys { get; } =
        new Dictionary<HotkeyAction, Hotkey>
        {
            [HotkeyAction.Convert] = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x4C), // L
            [HotkeyAction.Expand] = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x45),  // E
            [HotkeyAction.Picker] = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x20),  // Space
            [HotkeyAction.Capture] = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x4E), // N
            [HotkeyAction.AiImprove] = new(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x49), // I
            [HotkeyAction.UndoCorrection] = new(HotkeyModifiers.Shift, 0x08)                    // Shift+Backspace
        };

    /// <summary>Returns the effective hotkey for each action (saved value or the default).</summary>
    public async Task<IReadOnlyDictionary<HotkeyAction, Hotkey>> GetHotkeysAsync()
    {
        var result = new Dictionary<HotkeyAction, Hotkey>(DefaultHotkeys);
        foreach (var (action, key) in HotkeySettingKeys)
        {
            var raw = await _repository.GetAsync(key);
            if (raw is not null && Hotkey.TryParse(raw, out var parsed) && parsed.IsValid)
            {
                result[action] = parsed;
            }
        }

        return result;
    }

    public Task SetHotkeyAsync(HotkeyAction action, Hotkey hotkey) =>
        _repository.SetAsync(HotkeySettingKeys[action], hotkey.ToStorageString());

    private static readonly IReadOnlyDictionary<HotkeyAction, string> HotkeySettingKeys =
        new Dictionary<HotkeyAction, string>
        {
            [HotkeyAction.Convert] = SettingKeys.HotkeyConvert,
            [HotkeyAction.Expand] = SettingKeys.HotkeyExpand,
            [HotkeyAction.Picker] = SettingKeys.HotkeyPicker,
            [HotkeyAction.Capture] = SettingKeys.HotkeyCapture,
            [HotkeyAction.AiImprove] = SettingKeys.HotkeyAiImprove,
            [HotkeyAction.UndoCorrection] = SettingKeys.HotkeyUndoCorrection
        };

    private async Task<bool> GetBoolAsync(string key, bool defaultValue)
    {
        var raw = await _repository.GetAsync(key);
        return raw is null ? defaultValue : ParseBool(raw, defaultValue);
    }

    private Task SetBoolAsync(string key, bool value) =>
        _repository.SetAsync(key, value ? "true" : "false");

    private static bool ParseBool(string raw, bool fallback) => raw.Trim().ToLowerInvariant() switch
    {
        "true" or "1" or "yes" => true,
        "false" or "0" or "no" => false,
        _ => fallback
    };
}
