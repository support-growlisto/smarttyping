using SmartTyping.Application.Snippets;

namespace SmartTyping.Application.Abstractions;

/// <summary>Exports snippets to, and imports them from, a portable JSON document.</summary>
public interface ISnippetImportExportService
{
    /// <summary>Serializes all snippets (with category names) to a JSON string.</summary>
    Task<string> ExportAsync();

    /// <summary>
    /// Imports snippets from <paramref name="json"/>. Missing categories are created; existing
    /// triggers are handled per <paramref name="mode"/>. Returns a summary of what changed.
    /// </summary>
    Task<ImportSummary> ImportAsync(string json, ImportMode mode);
}
