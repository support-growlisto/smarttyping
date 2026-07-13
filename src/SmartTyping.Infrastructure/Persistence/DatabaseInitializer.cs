using Dapper;
using Microsoft.Extensions.Logging;
using SmartTyping.Domain.Enums;

namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// Creates the SQLite schema (idempotently) and seeds default settings, a category, and a few
/// sample snippets on first run. Safe to call on every startup.
/// </summary>
public sealed class DatabaseInitializer
{
    private const string SchemaSql = """
        PRAGMA journal_mode = WAL;
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS categories (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            Name       TEXT    NOT NULL,
            SortOrder  INTEGER NOT NULL DEFAULT 0,
            CreatedUtc TEXT    NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ux_categories_name ON categories(Name COLLATE NOCASE);

        CREATE TABLE IF NOT EXISTS snippets (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            Trigger    TEXT    NOT NULL,
            Content    TEXT    NOT NULL,
            CategoryId INTEGER NULL REFERENCES categories(Id) ON DELETE SET NULL,
            IsEnabled  INTEGER NOT NULL DEFAULT 1,
            UsageCount INTEGER NOT NULL DEFAULT 0,
            CreatedUtc TEXT    NOT NULL,
            UpdatedUtc TEXT    NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ux_snippets_trigger ON snippets(Trigger COLLATE NOCASE);

        CREATE TABLE IF NOT EXISTS usage_history (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            SnippetId INTEGER NOT NULL REFERENCES snippets(Id) ON DELETE CASCADE,
            UsedUtc   TEXT    NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_usage_snippet ON usage_history(SnippetId);

        CREATE TABLE IF NOT EXISTS app_settings (
            Key   TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );

        -- Words the user taught us by undoing a correction of them.
        CREATE TABLE IF NOT EXISTS learned_words (
            Word       TEXT    NOT NULL,
            IsThai     INTEGER NOT NULL,
            LearnedUtc TEXT    NOT NULL,
            PRIMARY KEY (Word, IsThai)
        );

        -- The personal dictionary: words the user types that no dictionary knows — names, jargon, the
        -- deliberate misspellings people use with each other. A word joins the vocabulary once it has
        -- been typed PersonalDictionary.Threshold times; until then this table is only a tally, and one
        -- that is pruned after PersonalDictionary.CandidateLifetimeDays so a word typed once and never
        -- again does not linger on disk.
        CREATE TABLE IF NOT EXISTS personal_words (
            Word         TEXT    NOT NULL,
            IsThai       INTEGER NOT NULL,
            Count        INTEGER NOT NULL,
            FirstSeenUtc TEXT    NOT NULL,
            LastSeenUtc  TEXT    NOT NULL,
            PRIMARY KEY (Word, IsThai)
        );
        """;

    private readonly ISqlConnectionFactory _factory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ISqlConnectionFactory factory, ILogger<DatabaseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>Creates the schema and seeds defaults if the database is empty.</summary>
    public void Initialize()
    {
        // Ensure UTC DateTime mapping is registered even when the initializer is used directly
        // (e.g. integration tests) rather than via AddInfrastructure.
        DapperConfiguration.Register();

        using var connection = _factory.CreateOpenConnection();
        connection.Execute(SchemaSql);
        SeedDefaults(connection);
        _logger.LogInformation("Database initialized at {ConnectionString}", _factory.ConnectionString);
    }

    private static void SeedDefaults(System.Data.IDbConnection connection)
    {
        var nowUtc = SqliteTime.ToStorage(DateTime.UtcNow);

        // Settings (insert only if missing).
        UpsertSettingIfMissing(connection, SettingKeys.SnippetExpansionEnabled, "true");
        UpsertSettingIfMissing(connection, SettingKeys.LanguageCorrectionEnabled, "true");
        UpsertSettingIfMissing(connection, SettingKeys.SchemaVersion, "1");

        // Seed a default category + sample snippets only when there are no snippets yet.
        var snippetCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM snippets;");
        if (snippetCount > 0)
        {
            return;
        }

        connection.Execute(
            "INSERT OR IGNORE INTO categories (Name, SortOrder, CreatedUtc) VALUES (@Name, 0, @CreatedUtc);",
            new { Name = "General", CreatedUtc = nowUtc });

        var generalId = connection.ExecuteScalar<long>(
            "SELECT Id FROM categories WHERE Name = 'General' COLLATE NOCASE;");

        var samples = new[]
        {
            new { Trigger = "/sig", Content = "Best regards,\nSmartTyping User" },
            new { Trigger = "/phone", Content = "08x-xxx-xxxx" },
            new { Trigger = "/date", Content = "Today is {date}" }
        };

        foreach (var s in samples)
        {
            connection.Execute(
                """
                INSERT INTO snippets (Trigger, Content, CategoryId, IsEnabled, UsageCount, CreatedUtc, UpdatedUtc)
                VALUES (@Trigger, @Content, @CategoryId, 1, 0, @CreatedUtc, @UpdatedUtc);
                """,
                new
                {
                    s.Trigger,
                    s.Content,
                    CategoryId = generalId,
                    CreatedUtc = nowUtc,
                    UpdatedUtc = nowUtc
                });
        }
    }

    private static void UpsertSettingIfMissing(System.Data.IDbConnection connection, string key, string value)
    {
        connection.Execute(
            "INSERT OR IGNORE INTO app_settings (Key, Value) VALUES (@Key, @Value);",
            new { Key = key, Value = value });
    }
}
