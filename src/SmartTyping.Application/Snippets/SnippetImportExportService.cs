using System.Text.Json;
using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;

namespace SmartTyping.Application.Snippets;

/// <summary>
/// Default <see cref="ISnippetImportExportService"/>: serializes snippets (with category names) to
/// JSON and imports them back, creating any missing categories and resolving trigger conflicts by
/// the chosen <see cref="ImportMode"/>.
/// </summary>
public sealed class SnippetImportExportService : ISnippetImportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ISnippetRepository _snippets;
    private readonly ICategoryRepository _categories;
    private readonly IDateTimeProvider _clock;

    public SnippetImportExportService(
        ISnippetRepository snippets,
        ICategoryRepository categories,
        IDateTimeProvider clock)
    {
        _snippets = snippets;
        _categories = categories;
        _clock = clock;
    }

    public async Task<string> ExportAsync()
    {
        var snippets = await _snippets.GetAllAsync();
        var categories = await _categories.GetAllAsync();
        var categoryNameById = categories.ToDictionary(c => c.Id, c => c.Name);

        var document = new SnippetsDocument
        {
            Snippets = snippets
                .Select(s => new SnippetDto
                {
                    Trigger = s.Trigger,
                    Content = s.Content,
                    Category = s.CategoryId is int id && categoryNameById.TryGetValue(id, out var name) ? name : null,
                    IsEnabled = s.IsEnabled
                })
                .ToList()
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public async Task<ImportSummary> ImportAsync(string json, ImportMode mode)
    {
        var document = JsonSerializer.Deserialize<SnippetsDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("The file is not a valid snippets document.");

        var summary = new ImportSummary();

        // Cache categories by name so we create each missing one only once.
        var categories = await _categories.GetAllAsync();
        var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in document.Snippets)
        {
            if (string.IsNullOrWhiteSpace(dto.Trigger) || string.IsNullOrWhiteSpace(dto.Content))
            {
                summary.Skipped++;
                continue;
            }

            var categoryId = await ResolveCategoryIdAsync(dto.Category, categoryIdByName);
            var existing = await _snippets.FindByTriggerAsync(dto.Trigger.Trim());
            var now = _clock.UtcNow;

            if (existing is null)
            {
                await _snippets.AddAsync(new Snippet
                {
                    Trigger = dto.Trigger.Trim(),
                    Content = dto.Content,
                    CategoryId = categoryId,
                    IsEnabled = dto.IsEnabled,
                    CreatedUtc = now,
                    UpdatedUtc = now
                });
                summary.Added++;
            }
            else if (mode == ImportMode.Overwrite)
            {
                existing.Content = dto.Content;
                existing.CategoryId = categoryId;
                existing.IsEnabled = dto.IsEnabled;
                existing.UpdatedUtc = now;
                await _snippets.UpdateAsync(existing);
                summary.Updated++;
            }
            else
            {
                summary.Skipped++;
            }
        }

        return summary;
    }

    private async Task<int?> ResolveCategoryIdAsync(string? name, Dictionary<string, int> cache)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        if (cache.TryGetValue(trimmed, out var id))
        {
            return id;
        }

        var newId = await _categories.AddAsync(new Category { Name = trimmed, CreatedUtc = _clock.UtcNow });
        cache[trimmed] = newId;
        return newId;
    }
}
