using System.Collections.ObjectModel;
using SmartTyping.UI.Mvvm;

namespace SmartTyping.UI.ViewModels;

/// <summary>One editable placeholder field (a <c>{input:Label}</c>).</summary>
public sealed class PlaceholderField : ObservableObject
{
    private string _value = string.Empty;

    public PlaceholderField(string label) => Label = label;

    public string Label { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

/// <summary>View model for the placeholder-input dialog shown during expansion of an {input:…} snippet.</summary>
public sealed class PlaceholderInputViewModel
{
    public PlaceholderInputViewModel(IReadOnlyList<string> labels)
    {
        Fields = new ObservableCollection<PlaceholderField>(labels.Select(l => new PlaceholderField(l)));
    }

    public ObservableCollection<PlaceholderField> Fields { get; }

    public IReadOnlyDictionary<string, string> ToValues() =>
        Fields.ToDictionary(f => f.Label, f => f.Value, StringComparer.Ordinal);
}
