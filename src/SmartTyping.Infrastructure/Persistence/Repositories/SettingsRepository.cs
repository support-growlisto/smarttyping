using Dapper;
using SmartTyping.Application.Abstractions;

namespace SmartTyping.Infrastructure.Persistence.Repositories;

/// <summary>Dapper/SQLite implementation of <see cref="ISettingsRepository"/>.</summary>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly ISqlConnectionFactory _factory;

    public SettingsRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync()
    {
        using var db = _factory.CreateOpenConnection();
        var rows = await db.QueryAsync<(string Key, string Value)>("SELECT Key, Value FROM app_settings;");
        return rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string?> GetAsync(string key)
    {
        using var db = _factory.CreateOpenConnection();
        return await db.QuerySingleOrDefaultAsync<string?>(
            "SELECT Value FROM app_settings WHERE Key = @key;", new { key });
    }

    public async Task SetAsync(string key, string value)
    {
        using var db = _factory.CreateOpenConnection();
        await db.ExecuteAsync(
            """
            INSERT INTO app_settings (Key, Value) VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """,
            new { key, value });
    }
}
