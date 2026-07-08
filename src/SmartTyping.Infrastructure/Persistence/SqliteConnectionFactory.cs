using System.Data;
using Microsoft.Data.Sqlite;

namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// <see cref="ISqlConnectionFactory"/> for SQLite via Microsoft.Data.Sqlite.
/// Enables foreign keys on every connection.
/// </summary>
public sealed class SqliteConnectionFactory : ISqlConnectionFactory
{
    public SqliteConnectionFactory(string? databaseFilePath = null)
    {
        var path = databaseFilePath ?? AppPaths.DatabaseFile;
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public string ConnectionString { get; }

    public IDbConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        return connection;
    }
}
