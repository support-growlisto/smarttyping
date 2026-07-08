using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;
using SmartTyping.UI.Localization;
using SmartTyping.UI.Mvvm;
using SmartTyping.UI.Services;

namespace SmartTyping.UI.ViewModels;

/// <summary>View model for the category-management window (add / rename / delete).</summary>
public sealed class CategoryManagerViewModel : ObservableObject
{
    private readonly ICategoryRepository _categories;
    private readonly IDateTimeProvider _clock;
    private readonly IDialogService _dialogs;

    private Category? _selected;

    public CategoryManagerViewModel(ICategoryRepository categories, IDateTimeProvider clock, IDialogService dialogs)
    {
        _categories = categories;
        _clock = clock;
        _dialogs = dialogs;

        AddCommand = new AsyncRelayCommand(AddAsync);
        RenameCommand = new AsyncRelayCommand(RenameAsync, () => Selected is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected is not null);
    }

    public ObservableCollection<Category> Categories { get; } = new();

    public Category? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public ICommand AddCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }

    public async Task LoadAsync()
    {
        var items = await _categories.GetAllAsync();
        Categories.Clear();
        foreach (var c in items)
        {
            Categories.Add(c);
        }
    }

    private async Task AddAsync()
    {
        var loc = LocalizationManager.Instance;
        var name = _dialogs.Prompt(loc["Cat_Title"], loc["Cat_NamePrompt"], string.Empty);
        if (string.IsNullOrWhiteSpace(name) || await IsDuplicateAsync(name, ignoreId: null))
        {
            return;
        }

        await _categories.AddAsync(new Category { Name = name.Trim(), CreatedUtc = _clock.UtcNow });
        await LoadAsync();
    }

    private async Task RenameAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var loc = LocalizationManager.Instance;
        var name = _dialogs.Prompt(loc["Cat_Title"], loc["Cat_NamePrompt"], Selected.Name);
        if (string.IsNullOrWhiteSpace(name) || await IsDuplicateAsync(name, Selected.Id))
        {
            return;
        }

        Selected.Name = name.Trim();
        await _categories.UpdateAsync(Selected);
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var loc = LocalizationManager.Instance;
        if (!_dialogs.Confirm(loc.Format("Cat_DeleteConfirm", Selected.Name), loc["Cat_Title"]))
        {
            return;
        }

        await _categories.DeleteAsync(Selected.Id);
        await LoadAsync();
    }

    private async Task<bool> IsDuplicateAsync(string name, int? ignoreId)
    {
        var trimmed = name.Trim();
        var all = await _categories.GetAllAsync();
        var clash = all.Any(c => c.Id != ignoreId && string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (clash)
        {
            var loc = LocalizationManager.Instance;
            _dialogs.ShowMessage(loc["Cat_Duplicate"], loc["Cat_Title"]);
        }

        return clash;
    }
}
