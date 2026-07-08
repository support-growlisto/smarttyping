using System.Data;

namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// Creates open ADO.NET connections to the SQLite database. Infrastructure-internal.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>The SQLite connection string in use.</summary>
    string ConnectionString { get; }

    /// <summary>Creates and opens a new connection. Caller disposes.</summary>
    IDbConnection CreateOpenConnection();
}
