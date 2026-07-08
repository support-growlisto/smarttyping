using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Domain.Enums;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>Integration tests for the settings repository against a temporary SQLite file.</summary>
public sealed class SettingsRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _factory;

    public SettingsRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"smarttyping-it-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_dbPath);
        new DatabaseInitializer(_factory, NullLogger<DatabaseInitializer>.Instance).Initialize();
    }

    [Fact]
    public async Task Initialize_SeedsDefaultSettings()
    {
        var repo = new SettingsRepository(_factory);

        Assert.Equal("true", await repo.GetAsync(SettingKeys.SnippetExpansionEnabled));
        Assert.Equal("true", await repo.GetAsync(SettingKeys.LanguageCorrectionEnabled));
    }

    [Fact]
    public async Task Set_InsertsThenUpdates()
    {
        var repo = new SettingsRepository(_factory);

        await repo.SetAsync(SettingKeys.LanguageCorrectionEnabled, "false");
        Assert.Equal("false", await repo.GetAsync(SettingKeys.LanguageCorrectionEnabled));

        await repo.SetAsync(SettingKeys.LanguageCorrectionEnabled, "true");
        Assert.Equal("true", await repo.GetAsync(SettingKeys.LanguageCorrectionEnabled));
    }

    [Fact]
    public async Task GetAsync_UnknownKey_ReturnsNull()
    {
        var repo = new SettingsRepository(_factory);
        Assert.Null(await repo.GetAsync("does-not-exist"));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                var path = _dbPath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
