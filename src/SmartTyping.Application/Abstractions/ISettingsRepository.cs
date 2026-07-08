namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Key/value persistence port for application settings (the <c>app_settings</c> table).
/// </summary>
public interface ISettingsRepository
{
    /// <summary>Returns all settings as a key/value map.</summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync();

    /// <summary>Returns a single setting value, or null if absent.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Inserts or updates a setting.</summary>
    Task SetAsync(string key, string value);
}
