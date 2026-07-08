using System.Data;
using Dapper;
using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;

namespace SmartTyping.Infrastructure.Persistence.Repositories;

/// <summary>Dapper/SQLite implementation of <see cref="ISnippetRepository"/>.</summary>
public sealed class SnippetRepository : ISnippetRepository
{
    private readonly ISqlConnectionFactory _factory;

    public SnippetRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Snippet>> GetAllAsync()
    {
        using var db = _factory.CreateOpenConnection();
        var rows = await db.QueryAsync<Snippet>(
            "SELECT Id, Trigger, Content, CategoryId, IsEnabled, UsageCount, CreatedUtc, UpdatedUtc FROM snippets ORDER BY Trigger COLLATE NOCASE;");
        return rows.ToList();
    }

    public async Task<Snippet?> GetByIdAsync(int id)
    {
        using var db = _factory.CreateOpenConnection();
        return await db.QuerySingleOrDefaultAsync<Snippet>(
            "SELECT Id, Trigger, Content, CategoryId, IsEnabled, UsageCount, CreatedUtc, UpdatedUtc FROM snippets WHERE Id = @id;",
            new { id });
    }

    public async Task<Snippet?> FindByTriggerAsync(string trigger)
    {
        using var db = _factory.CreateOpenConnection();
        return await db.QuerySingleOrDefaultAsync<Snippet>(
            "SELECT Id, Trigger, Content, CategoryId, IsEnabled, UsageCount, CreatedUtc, UpdatedUtc FROM snippets WHERE Trigger = @trigger COLLATE NOCASE;",
            new { trigger });
    }

    public async Task<int> AddAsync(Snippet snippet)
    {
        using var db = _factory.CreateOpenConnection();
        var id = await db.ExecuteScalarAsync<long>(
            """
            INSERT INTO snippets (Trigger, Content, CategoryId, IsEnabled, UsageCount, CreatedUtc, UpdatedUtc)
            VALUES (@Trigger, @Content, @CategoryId, @IsEnabled, @UsageCount, @CreatedUtc, @UpdatedUtc);
            SELECT last_insert_rowid();
            """,
            ToRow(snippet));
        return (int)id;
    }

    public async Task UpdateAsync(Snippet snippet)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            """
            UPDATE snippets
            SET Trigger = @Trigger, Content = @Content, CategoryId = @CategoryId,
                IsEnabled = @IsEnabled, UsageCount = @UsageCount, UpdatedUtc = @UpdatedUtc
            WHERE Id = @Id;
            """,
            ToRow(snippet));
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync("DELETE FROM snippets WHERE Id = @id;", new { id });
    }

    public async Task RegisterUsageAsync(int snippetId, DateTime usedUtc)
    {
        using var db = _factory.CreateOpenConnection();
        using var tx = db.BeginTransaction();

        var usedUtcText = SqliteTime.ToStorage(usedUtc);
        await db.ExecuteAsync(
            "UPDATE snippets SET UsageCount = UsageCount + 1, UpdatedUtc = @usedUtc WHERE Id = @snippetId;",
            new { snippetId, usedUtc = usedUtcText }, tx);

        await db.ExecuteAsync(
            "INSERT INTO usage_history (SnippetId, UsedUtc) VALUES (@snippetId, @usedUtc);",
            new { snippetId, usedUtc = usedUtcText }, tx);

        tx.Commit();
    }

    public async Task ResetUsageAsync(DateTime updatedUtc)
    {
        using var db = _factory.CreateOpenConnection();
        using var tx = db.BeginTransaction();

        await db.ExecuteAsync(
            "UPDATE snippets SET UsageCount = 0, UpdatedUtc = @updatedUtc;",
            new { updatedUtc = SqliteTime.ToStorage(updatedUtc) }, tx);
        await db.ExecuteAsync("DELETE FROM usage_history;", transaction: tx);

        tx.Commit();
    }

    private static object ToRow(Snippet s) => new
    {
        s.Id,
        s.Trigger,
        s.Content,
        s.CategoryId,
        IsEnabled = s.IsEnabled ? 1 : 0,
        s.UsageCount,
        CreatedUtc = SqliteTime.ToStorage(s.CreatedUtc),
        UpdatedUtc = SqliteTime.ToStorage(s.UpdatedUtc)
    };
}
