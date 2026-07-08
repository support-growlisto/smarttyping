using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Stats;
using SmartTyping.UI.Localization;
using SmartTyping.UI.Mvvm;
using SmartTyping.UI.Services;

namespace SmartTyping.UI.ViewModels;

/// <summary>View model for the usage-statistics window.</summary>
public sealed class StatsViewModel : ObservableObject
{
    private readonly ISnippetRepository _snippets;
    private readonly IDateTimeProvider _clock;
    private readonly IDialogService _dialogs;

    private int _totalSnippets;
    private string _enabledText = string.Empty;
    private int _totalExpansions;
    private string _timeSavedText = string.Empty;

    public StatsViewModel(ISnippetRepository snippets, IDateTimeProvider clock, IDialogService dialogs)
    {
        _snippets = snippets;
        _clock = clock;
        _dialogs = dialogs;
        ClearCommand = new AsyncRelayCommand(ClearAsync);
    }

    public ObservableCollection<SnippetUsage> TopUsed { get; } = new();

    public ICommand ClearCommand { get; }

    public int TotalSnippets { get => _totalSnippets; private set => SetProperty(ref _totalSnippets, value); }
    public string EnabledText { get => _enabledText; private set => SetProperty(ref _enabledText, value); }
    public int TotalExpansions { get => _totalExpansions; private set => SetProperty(ref _totalExpansions, value); }
    public string TimeSavedText { get => _timeSavedText; private set => SetProperty(ref _timeSavedText, value); }

    public bool HasUsage => TopUsed.Count > 0;

    public async Task LoadAsync()
    {
        var snippets = await _snippets.GetAllAsync();
        var stats = StatsCalculator.Compute(snippets);
        var loc = LocalizationManager.Instance;

        TotalSnippets = stats.TotalSnippets;
        EnabledText = loc.Format("Stats_Enabled", stats.EnabledSnippets);
        TotalExpansions = stats.TotalExpansions;
        TimeSavedText = FormatDuration(stats.EstimatedSecondsSaved);

        TopUsed.Clear();
        foreach (var u in stats.TopUsed)
        {
            TopUsed.Add(u);
        }

        OnPropertyChanged(nameof(HasUsage));
    }

    private static string FormatDuration(int seconds)
    {
        var loc = LocalizationManager.Instance;
        return seconds >= 60
            ? loc.Format("Stats_Minutes", Math.Round(seconds / 60.0, 1))
            : loc.Format("Stats_Seconds", seconds);
    }

    private async Task ClearAsync()
    {
        var loc = LocalizationManager.Instance;
        if (!_dialogs.Confirm(loc["Stats_ClearConfirm"], loc["Stats_Title"]))
        {
            return;
        }

        await _snippets.ResetUsageAsync(_clock.UtcNow);
        await LoadAsync();
    }
}
