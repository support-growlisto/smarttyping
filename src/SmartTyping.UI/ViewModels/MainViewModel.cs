using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Snippets;
using SmartTyping.Domain.Entities;
using SmartTyping.UI.Localization;
using SmartTyping.UI.Mvvm;
using SmartTyping.UI.Services;

namespace SmartTyping.UI.ViewModels;

/// <summary>Root view model for the main window: snippet list, categories, and CRUD commands.</summary>
public sealed class MainViewModel : ObservableObject
{
    private static string JsonFilter => LocalizationManager.Instance["File_JsonFilter"];

    private readonly ISnippetRepository _snippets;
    private readonly ICategoryRepository _categories;
    private readonly IDateTimeProvider _clock;
    private readonly ITemplateEngine _templateEngine;
    private readonly ISnippetImportExportService _importExport;
    private readonly IDialogService _dialogs;

    private readonly List<SnippetItemViewModel> _allItems = new();

    private string _searchText = string.Empty;
    private CategoryFilter? _selectedCategory;
    private SnippetItemViewModel? _selectedSnippet;
    private string _statusMessage = string.Empty;

    public MainViewModel(
        ISnippetRepository snippets,
        ICategoryRepository categories,
        IDateTimeProvider clock,
        ITemplateEngine templateEngine,
        ISnippetImportExportService importExport,
        IDialogService dialogs,
        SettingsViewModel settings)
    {
        _snippets = snippets;
        _categories = categories;
        _clock = clock;
        _templateEngine = templateEngine;
        _importExport = importExport;
        _dialogs = dialogs;
        Settings = settings;

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(EditSelectedAsync, () => SelectedSnippet is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedSnippet is not null);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        StatsCommand = new RelayCommand(OpenStats);
        ManageCategoriesCommand = new AsyncRelayCommand(ManageCategoriesAsync);
    }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<SnippetItemViewModel> Snippets { get; } = new();

    public ObservableCollection<CategoryFilter> CategoryFilters { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand StatsCommand { get; }
    public ICommand ManageCategoriesCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public CategoryFilter? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    public SnippetItemViewModel? SelectedSnippet
    {
        get => _selectedSnippet;
        set => SetProperty(ref _selectedSnippet, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True when there are no snippets at all — drives the empty-state hint.</summary>
    public bool IsEmpty => _allItems.Count == 0;

    public async Task LoadAsync()
    {
        await Settings.LoadAsync();
        await LoadCategoriesAsync();
        await LoadSnippetsAsync();
        StatusMessage = LocalizationManager.Instance.Format("Status_Loaded", _allItems.Count);
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categories.GetAllAsync();
        CategoryFilters.Clear();
        var allFilter = new CategoryFilter(null, LocalizationManager.Instance["Main_All"]);
        CategoryFilters.Add(allFilter);
        foreach (var c in categories)
        {
            CategoryFilters.Add(new CategoryFilter(c.Id, c.Name));
        }

        _selectedCategory = allFilter;
        OnPropertyChanged(nameof(SelectedCategory));
    }

    private async Task LoadSnippetsAsync()
    {
        var snippets = await _snippets.GetAllAsync();
        _allItems.Clear();
        foreach (var s in snippets)
        {
            _allItems.Add(new SnippetItemViewModel(s, OnSnippetEnabledChangedAsync));
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchText?.Trim() ?? string.Empty;
        var categoryId = SelectedCategory?.CategoryId;

        IEnumerable<SnippetItemViewModel> query = _allItems;

        if (categoryId is not null)
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        if (search.Length > 0)
        {
            query = query.Where(i =>
                i.Trigger.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Model.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Snippets.Clear();
        foreach (var item in query)
        {
            Snippets.Add(item);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task OnSnippetEnabledChangedAsync(SnippetItemViewModel item)
    {
        await _snippets.UpdateAsync(item.Model);
        StatusMessage = LocalizationManager.Instance.Format(item.IsEnabled ? "Status_Enabled" : "Status_Disabled", item.Trigger);
    }

    private Task AddAsync() => AddFromContentAsync(null);

    /// <summary>Opens the Add dialog, optionally pre-filled with <paramref name="initialContent"/>
    /// (used by "add snippet from selection").</summary>
    public async Task AddFromContentAsync(string? initialContent)
    {
        var categories = await _categories.GetAllAsync();
        var editVm = new SnippetEditViewModel(_snippets, _clock, _templateEngine, categories, existing: null, initialContent);
        if (_dialogs.ShowSnippetEditor(editVm))
        {
            await LoadSnippetsAsync();
            StatusMessage = LocalizationManager.Instance["Status_Added"];
        }
    }

    private async Task EditSelectedAsync()
    {
        if (SelectedSnippet is null)
        {
            return;
        }

        var categories = await _categories.GetAllAsync();
        var editVm = new SnippetEditViewModel(_snippets, _clock, _templateEngine, categories, SelectedSnippet.Model);
        if (_dialogs.ShowSnippetEditor(editVm))
        {
            await LoadSnippetsAsync();
            StatusMessage = LocalizationManager.Instance["Status_Updated"];
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedSnippet is null)
        {
            return;
        }

        var confirmMessage = LocalizationManager.Instance.Format("Dlg_DeleteMsg", SelectedSnippet.Trigger);
        if (!_dialogs.Confirm(confirmMessage, LocalizationManager.Instance["Dlg_DeleteTitle"]))
        {
            return;
        }

        await _snippets.DeleteAsync(SelectedSnippet.Id);
        await LoadSnippetsAsync();
        StatusMessage = LocalizationManager.Instance["Status_Deleted"];
    }

    private void OpenStats()
    {
        var vm = new StatsViewModel(_snippets, _clock, _dialogs);
        _dialogs.ShowStats(vm);
    }

    private async Task ManageCategoriesAsync()
    {
        var vm = new CategoryManagerViewModel(_categories, _clock, _dialogs);
        _dialogs.ShowCategoryManager(vm);
        // Categories may have changed — refresh the filter list and grid.
        await LoadCategoriesAsync();
        await LoadSnippetsAsync();
    }

    private async Task ExportAsync()
    {
        var loc = LocalizationManager.Instance;
        var path = _dialogs.SaveFile(JsonFilter, loc["Dlg_ExportTitle"], "smarttyping-snippets.json");
        if (path is null)
        {
            return;
        }

        try
        {
            var json = await _importExport.ExportAsync();
            await File.WriteAllTextAsync(path, json);
            StatusMessage = loc.Format("Status_Exported", _allItems.Count, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusMessage = loc.Format("Status_ExportFailed", ex.Message);
        }
    }

    private async Task ImportAsync()
    {
        var loc = LocalizationManager.Instance;
        var path = _dialogs.OpenFile(JsonFilter, loc["Dlg_ImportTitle"]);
        if (path is null)
        {
            return;
        }

        // Yes = overwrite existing triggers, No = keep existing (skip), Cancel = abort.
        var choice = _dialogs.ConfirmYesNoCancel(loc["Dlg_ImportPrompt"], loc["Dlg_ImportTitle"]);
        if (choice is null)
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var summary = await _importExport.ImportAsync(json, choice.Value ? ImportMode.Overwrite : ImportMode.Skip);
            await LoadCategoriesAsync();
            await LoadSnippetsAsync();
            StatusMessage = loc.Format("Status_ImportDone", summary);
        }
        catch (Exception ex)
        {
            StatusMessage = loc.Format("Status_ImportFailed", ex.Message);
        }
    }
}

/// <summary>An entry in the category filter list ("All" plus each category).</summary>
public sealed class CategoryFilter
{
    public CategoryFilter(int? categoryId, string name)
    {
        CategoryId = categoryId;
        Name = name;
    }

    public static CategoryFilter All { get; } = new(null, "All");

    public int? CategoryId { get; }

    public string Name { get; }

    public override string ToString() => Name;
}
