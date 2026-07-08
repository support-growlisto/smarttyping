using SmartTyping.Domain.Entities;
using SmartTyping.UI.Mvvm;

namespace SmartTyping.UI.ViewModels;

/// <summary>Row view model wrapping a <see cref="Snippet"/> for the snippet grid.</summary>
public sealed class SnippetItemViewModel : ObservableObject
{
    private bool _isEnabled;

    public SnippetItemViewModel(Snippet model, Func<SnippetItemViewModel, Task> onEnabledChanged)
    {
        Model = model;
        _isEnabled = model.IsEnabled;
        OnEnabledChanged = onEnabledChanged;
    }

    public Snippet Model { get; }

    /// <summary>Callback invoked (fire-and-forget) when the user toggles <see cref="IsEnabled"/>.</summary>
    public Func<SnippetItemViewModel, Task> OnEnabledChanged { get; }

    public int Id => Model.Id;

    public string Trigger => Model.Trigger;

    /// <summary>Single-line preview of the content for the grid.</summary>
    public string ContentPreview
    {
        get
        {
            var oneLine = Model.Content.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length > 80 ? oneLine[..80] + "…" : oneLine;
        }
    }

    public int? CategoryId => Model.CategoryId;

    public int UsageCount => Model.UsageCount;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                Model.IsEnabled = value;
                _ = OnEnabledChanged(this);
            }
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Trigger));
        OnPropertyChanged(nameof(ContentPreview));
        OnPropertyChanged(nameof(UsageCount));
    }
}
