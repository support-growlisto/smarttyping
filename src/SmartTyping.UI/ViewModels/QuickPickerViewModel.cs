using System.Collections.ObjectModel;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Snippets;
using SmartTyping.UI.Mvvm;

namespace SmartTyping.UI.ViewModels;

/// <summary>
/// View model for the quick-picker: a searchable list of enabled snippets. Ranking/filtering lives
/// in <see cref="SnippetSearch"/> (Application) so it can be unit-tested without the UI.
/// </summary>
public sealed class QuickPickerViewModel : ObservableObject
{
    private readonly ISnippetRepository _snippets;
    private readonly List<SnippetSearchItem> _all = new();

    private string _query = string.Empty;
    private SnippetSearchItem? _selected;

    public QuickPickerViewModel(ISnippetRepository snippets) => _snippets = snippets;

    public ObservableCollection<SnippetSearchItem> Results { get; } = new();

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                ApplyFilter();
            }
        }
    }

    public SnippetSearchItem? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public async Task LoadAsync()
    {
        var snippets = await _snippets.GetAllAsync();
        _all.Clear();
        foreach (var s in snippets.Where(s => s.IsEnabled))
        {
            _all.Add(new SnippetSearchItem(s.Id, s.Trigger, Preview(s.Content), s.UsageCount));
        }

        ApplyFilter();
    }

    /// <summary>Moves the selection by <paramref name="delta"/> rows (keyboard navigation).</summary>
    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            return;
        }

        var index = Selected is null ? -1 : Results.IndexOf(Selected);
        index = Math.Clamp(index + delta, 0, Results.Count - 1);
        Selected = Results[index];
    }

    private void ApplyFilter()
    {
        Results.Clear();
        foreach (var item in SnippetSearch.Filter(_all, Query))
        {
            Results.Add(item);
        }

        Selected = Results.FirstOrDefault();
    }

    private static string Preview(string content)
    {
        var oneLine = content.Replace("\r", " ").Replace("\n", " ").Trim();
        return oneLine.Length > 90 ? oneLine[..90] + "…" : oneLine;
    }
}
