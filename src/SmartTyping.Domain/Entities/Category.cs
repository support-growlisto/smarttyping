namespace SmartTyping.Domain.Entities;

/// <summary>
/// A grouping label for snippets (e.g. "Work", "Personal").
/// </summary>
public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedUtc { get; set; }
}
