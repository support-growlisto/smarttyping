namespace SmartTyping.Application.Snippets;

/// <summary>Serializable envelope for exporting/importing snippets as JSON.</summary>
public sealed class SnippetsDocument
{
    /// <summary>Schema version, for forward compatibility.</summary>
    public int Version { get; set; } = 1;

    public List<SnippetDto> Snippets { get; set; } = new();
}

/// <summary>A single exported snippet. Category is referenced by name (portable across databases).</summary>
public sealed class SnippetDto
{
    public string Trigger { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Category { get; set; }

    public bool IsEnabled { get; set; } = true;
}

/// <summary>How to treat a snippet whose trigger already exists on import.</summary>
public enum ImportMode
{
    /// <summary>Keep the existing snippet, ignore the imported one.</summary>
    Skip,

    /// <summary>Replace the existing snippet's content/category/enabled with the imported values.</summary>
    Overwrite
}

/// <summary>Outcome of an import.</summary>
public sealed class ImportSummary
{
    public int Added { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public int Total => Added + Updated + Skipped;

    public override string ToString() => $"Added {Added}, updated {Updated}, skipped {Skipped}.";
}
