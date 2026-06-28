using System.Data;
using Microsoft.Data.Sqlite;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// SQLite fixture. SQLite is in-process — no container — so it backs the matrix
/// with a temporary database file created and seeded once per test class.
/// </summary>
public sealed class SqliteFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"sqlartisan_it_{Guid.NewGuid():N}.db");

    public Dbms Dbms => Dbms.Sqlite;

    private string ConnectionString => $"Data Source={_databasePath}";

    public IDbConnection OpenConnection()
    {
        SqliteConnection connection = new(ConnectionString);
        connection.Open();
        return connection;
    }

    public Task InitializeAsync()
    {
        using IDbConnection connection = OpenConnection();
        TestSchema.Apply(connection, TestSchema.StandardDdl);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }
}
