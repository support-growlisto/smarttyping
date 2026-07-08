using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;
using SmartTyping.Domain.ValueObjects;
using SmartTyping.UI.Mvvm;

namespace SmartTyping.UI.ViewModels;

/// <summary>View model for the add/edit snippet dialog.</summary>
public sealed class SnippetEditViewModel : ObservableObject
{
    private readonly ISnippetRepository _snippets;
    private readonly IDateTimeProvider _clock;
    private readonly ITemplateEngine _templateEngine;
    private readonly Snippet _model;
    private readonly bool _isNew;

    private string _trigger;
    private string _content;
    private bool _isEnabled;
    private Category? _selectedCategory;
    private string? _error;
    private string _previewText = string.Empty;

    public SnippetEditViewModel(
        ISnippetRepository snippets,
        IDateTimeProvider clock,
        ITemplateEngine templateEngine,
        IReadOnlyList<Category> categories,
        Snippet? existing,
        string? initialContent = null)
    {
        _snippets = snippets;
        _clock = clock;
        _templateEngine = templateEngine;
        _isNew = existing is null;
        _model = existing ?? new Snippet();

        _trigger = _model.Trigger;
        _content = _isNew && !string.IsNullOrEmpty(initialContent) ? initialContent : _model.Content;
        _isEnabled = _model.IsEnabled;

        Categories = new ObservableCollection<Category>(categories);
        _selectedCategory = categories.FirstOrDefault(c => c.Id == _model.CategoryId);

        PreviewCommand = new AsyncRelayCommand(PreviewAsync);
    }

    public ObservableCollection<Category> Categories { get; }

    public ICommand PreviewCommand { get; }

    /// <summary>The rendered preview of <see cref="Content"/>, with a ▮ marker at the caret position.</summary>
    public string PreviewText
    {
        get => _previewText;
        private set => SetProperty(ref _previewText, value);
    }

    public string Title => Localization.LocalizationManager.Instance[_isNew ? "Edit_Title_Add" : "Edit_Title_Edit"];

    public string Trigger
    {
        get => _trigger;
        set => SetProperty(ref _trigger, value);
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string? Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    /// <summary>Renders the current content (template variables applied) into <see cref="PreviewText"/>.</summary>
    private async Task PreviewAsync()
    {
        if (string.IsNullOrEmpty(Content))
        {
            PreviewText = string.Empty;
            return;
        }

        var rendered = await _templateEngine.RenderAsync(Content);
        PreviewText = rendered.CursorOffset is int offset && offset <= rendered.Text.Length
            ? rendered.Text.Insert(offset, "▮")
            : rendered.Text;
    }

    /// <summary>Validates and persists the snippet. Returns true on success.</summary>
    public async Task<bool> SaveAsync()
    {
        Error = null;

        var triggerResult = Domain.ValueObjects.Trigger.Create(Trigger);
        if (triggerResult.IsFailure)
        {
            Error = triggerResult.Error;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            Error = "Content cannot be empty.";
            return false;
        }

        var normalizedTrigger = triggerResult.Value.Value;

        // Enforce unique trigger (case-insensitive), ignoring this same snippet when editing.
        var clash = await _snippets.FindByTriggerAsync(normalizedTrigger);
        if (clash is not null && clash.Id != _model.Id)
        {
            Error = $"A snippet with trigger '{normalizedTrigger}' already exists.";
            return false;
        }

        var now = _clock.UtcNow;
        _model.Trigger = normalizedTrigger;
        _model.Content = Content;
        _model.IsEnabled = IsEnabled;
        _model.CategoryId = SelectedCategory?.Id;
        _model.UpdatedUtc = now;

        if (_isNew)
        {
            _model.CreatedUtc = now;
            await _snippets.AddAsync(_model);
        }
        else
        {
            await _snippets.UpdateAsync(_model);
        }

        return true;
    }
}
