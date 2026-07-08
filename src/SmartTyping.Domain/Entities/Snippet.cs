namespace SmartTyping.Domain.Entities;

/// <summary>
/// A text-expansion snippet: a short trigger that expands into stored content,
/// which may contain template variables such as <c>{date}</c>.
/// </summary>
public sealed class Snippet
{
    public int Id { get; set; }

    /// <summary>The trigger token, e.g. <c>/phone</c>. Unique (case-insensitive).</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>The raw content, possibly containing template variables.</summary>
    public string Content { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    /// <summary>Disabled snippets are never expanded.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Number of times this snippet has been expanded.</summary>
    public int UsageCount { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>Records one use: increments the counter and touches the update timestamp.</summary>
    public void RegisterUse(DateTime whenUtc)
    {
        UsageCount++;
        UpdatedUtc = whenUtc;
    }
}
