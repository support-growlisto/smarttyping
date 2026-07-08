namespace SmartTyping.Domain.Entities;

/// <summary>
/// A single record that a snippet was expanded at a point in time. Kept local; user can clear it.
/// </summary>
public sealed class UsageHistory
{
    public int Id { get; set; }

    public int SnippetId { get; set; }

    public DateTime UsedUtc { get; set; }
}
