using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Enums;

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

    /// <summary>The UI language code (e.g. "th", "en"). Defaults to Thai.</summary>
    public async Task<string> GetLanguageAsync()
    {
        var value = await _repository.GetAsync(SettingKeys.Language);
        return string.IsNullOrWhiteSpace(value) ? "th" : value.Trim();
    }

    public Task SetLanguageAsync(string language) =>
        _repository.SetAsync(SettingKeys.Language, language);

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
