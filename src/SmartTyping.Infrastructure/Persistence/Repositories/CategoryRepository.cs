using Dapper;
using SmartTyping.Application.Abstractions;
using SmartTyping.Domain.Entities;

namespace SmartTyping.Infrastructure.Persistence.Repositories;

/// <summary>Dapper/SQLite implementation of <see cref="ICategoryRepository"/>.</summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CategoryRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        using var db = _factory.CreateOpenConnection();
        var rows = await db.QueryAsync<Category>(
            "SELECT Id, Name, SortOrder, CreatedUtc FROM categories ORDER BY SortOrder, Name COLLATE NOCASE;");
        return rows.ToList();
    }

    public async Task<int> AddAsync(Category category)
    {
        using var db = _factory.CreateOpenConnection();
        var id = await db.ExecuteScalarAsync<long>(
            """
            INSERT INTO categories (Name, SortOrder, CreatedUtc) VALUES (@Name, @SortOrder, @CreatedUtc);
            SELECT last_insert_rowid();
            """,
            new { category.Name, category.SortOrder, CreatedUtc = SqliteTime.ToStorage(category.CreatedUtc) });
        return (int)id;
    }

    public async Task UpdateAsync(Category category)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            "UPDATE categories SET Name = @Name, SortOrder = @SortOrder WHERE Id = @Id;",
            new { category.Name, category.SortOrder, category.Id });
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync("DELETE FROM categories WHERE Id = @id;", new { id });
    }
}
