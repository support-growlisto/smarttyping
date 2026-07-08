using SmartTyping.Domain.Entities;

namespace SmartTyping.Application.Abstractions;

/// <summary>
/// Persistence port for snippets. Implemented in Infrastructure with Dapper/SQLite.
/// </summary>
public interface ISnippetRepository
{
    Task<IReadOnlyList<Snippet>> GetAllAsync();

    Task<Snippet?> GetByIdAsync(int id);

    /// <summary>Finds a snippet by its trigger, case-insensitively. Returns null if not found.</summary>
    Task<Snippet?> FindByTriggerAsync(string trigger);

    /// <summary>Inserts a new snippet and returns its assigned id.</summary>
    Task<int> AddAsync(Snippet snippet);

    Task UpdateAsync(Snippet snippet);

    Task DeleteAsync(int id);

    /// <summary>
    /// Records one expansion: increments the usage count and appends a usage-history row.
    /// A single atomic operation.
    /// </summary>
    Task RegisterUsageAsync(int snippetId, DateTime usedUtc);
}
