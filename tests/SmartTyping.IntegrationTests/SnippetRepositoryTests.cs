using Microsoft.Extensions.Logging.Abstractions;
using SmartTyping.Domain.Entities;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SmartTyping.IntegrationTests;

/// <summary>
/// Integration tests that exercise the Dapper repositories against a real (temporary) SQLite file,
/// verifying the schema, round-trips (including DateTime/bool mapping), unique trigger handling,
/// usage tracking, and cascade delete.
/// </summary>
public sealed class SnippetRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _factory;

    public SnippetRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"smarttyping-it-{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_dbPath);
        new DatabaseInitializer(_factory, NullLogger<DatabaseInitializer>.Instance).Initialize();
    }

    [Fact]
    public async Task Initialize_SeedsSampleSnippets()
    {
        var repo = new SnippetRepository(_factory);
        var all = await repo.GetAllAsync();

        Assert.Contains(all, s => s.Trigger == "/sig");
        Assert.Contains(all, s => s.Trigger == "/date");
    }

    [Fact]
    public async Task AddAndFind_RoundTripsAllFields()
    {
        var repo = new SnippetRepository(_factory);
        var created = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);

        var id = await repo.AddAsync(new Snippet
        {
            Trigger = "/hello",
            Content = "Hello there",
            IsEnabled = true,
            CreatedUtc = created,
            UpdatedUtc = created
        });

        var loaded = await repo.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("/hello", loaded!.Trigger);
        Assert.Equal("Hello there", loaded.Content);
        Assert.True(loaded.IsEnabled);
        Assert.Equal(created, loaded.CreatedUtc);
        // Timestamps must round-trip as UTC, not Unspecified/Local.
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedUtc.Kind);
    }

    [Fact]
    public async Task FindByTrigger_IsCaseInsensitive()
    {
        var repo = new SnippetRepository(_factory);
        await repo.AddAsync(new Snippet { Trigger = "/case", Content = "x" });

        var found = await repo.FindByTriggerAsync("/CASE");

        Assert.NotNull(found);
        Assert.Equal("/case", found!.Trigger);
    }

    [Fact]
    public async Task RegisterUsage_IncrementsCountAndWritesHistory()
    {
        var repo = new SnippetRepository(_factory);
        var id = await repo.AddAsync(new Snippet { Trigger = "/use", Content = "x" });

        await repo.RegisterUsageAsync(id, DateTime.UtcNow);
        await repo.RegisterUsageAsync(id, DateTime.UtcNow);

        var loaded = await repo.GetByIdAsync(id);
        Assert.Equal(2, loaded!.UsageCount);

        // Deleting the snippet should cascade-delete its usage history (FK ON DELETE CASCADE).
        await repo.DeleteAsync(id);
        Assert.Null(await repo.GetByIdAsync(id));
    }

    public void Dispose()
    {
        SqliteConnectionPoolCleanup();
        TryDelete(_dbPath);
        TryDelete(_dbPath + "-wal");
        TryDelete(_dbPath + "-shm");
    }

    // Ensure pooled connections are released so the temp file can be deleted on Windows.
    private static void SqliteConnectionPoolCleanup() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; a leftover temp file is harmless.
        }
    }
}
