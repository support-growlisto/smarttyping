namespace SmartTyping.Domain.Enums;

/// <summary>
/// Canonical keys for rows in the <c>app_settings</c> table. Kept as constants (not an enum)
/// because they are persisted as strings and must stay stable across versions.
/// </summary>
public static class SettingKeys
{
    public const string SnippetExpansionEnabled = "SnippetExpansionEnabled";
    public const string LanguageCorrectionEnabled = "LanguageCorrectionEnabled";
    public const string StartWithWindows = "StartWithWindows";
    public const string OnboardingCompleted = "OnboardingCompleted";
    public const string Language = "Language";
    public const string Theme = "Theme";
    public const string HotkeyConvert = "HotkeyConvert";
    public const string HotkeyExpand = "HotkeyExpand";
    public const string HotkeyPicker = "HotkeyPicker";
    public const string HotkeyCapture = "HotkeyCapture";
    public const string CheckForUpdates = "CheckForUpdates";
    public const string AutoCorrectSuggest = "AutoCorrectSuggest";
    public const string AutoCorrectAuto = "AutoCorrectAuto";
    public const string AutoExpandSnippets = "AutoExpandSnippets";
    public const string HotkeyAiImprove = "HotkeyAiImprove";
    public const string AiProvider = "AiProvider";
    public const string AiApiKey = "AiApiKey";
    public const string SchemaVersion = "SchemaVersion";
}
