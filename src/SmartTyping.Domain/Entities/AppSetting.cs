namespace SmartTyping.Domain.Entities;

/// <summary>
/// A key/value application setting persisted in SQLite. Values are stored as strings;
/// callers parse to the target type. See <see cref="Enums.SettingKeys"/> for known keys.
/// </summary>
public sealed class AppSetting
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
