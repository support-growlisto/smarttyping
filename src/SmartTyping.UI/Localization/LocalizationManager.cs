using System.ComponentModel;

namespace SmartTyping.UI.Localization;

/// <summary>
/// Runtime-switchable UI localization. XAML binds to the indexer:
/// <c>{Binding [Key], Source={x:Static loc:LocalizationManager.Instance}}</c>.
/// Switching language raises a change on the indexer so every bound string refreshes live.
/// Only UI strings are affected — date/number formatting still follows the system culture.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public const string Thai = "th";
    public const string English = "en";

    public static LocalizationManager Instance { get; } = new();

    private string _language = Thai;

    private LocalizationManager()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage => _language;

    /// <summary>Localized string for <paramref name="key"/> in the current language (falls back to English, then the key).</summary>
    public string this[string key]
    {
        get
        {
            if (Strings.Table.TryGetValue(key, out var pair))
            {
                return _language == Thai ? pair.Th : pair.En;
            }

            return key;
        }
    }

    /// <summary>Localized, formatted string (e.g. status messages with arguments).</summary>
    public string Format(string key, params object[] args) => string.Format(this[key], args);

    public void SetLanguage(string language)
    {
        var normalized = language == English ? English : Thai;
        if (_language == normalized)
        {
            return;
        }

        _language = normalized;
        // Refresh every indexer binding at once.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Windows.Data.Binding.IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
    }
}
