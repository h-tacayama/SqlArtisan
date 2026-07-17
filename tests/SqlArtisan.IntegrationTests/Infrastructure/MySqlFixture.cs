using System.Data;
using MySqlConnector;
using Testcontainers.MySql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>MySQL fixture, backed by a Testcontainers <c>mysql</c> container (8.0 for window functions).</summary>
public sealed class MySqlFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .Build();

    public Dbms Dbms => Dbms.MySql;

    /// <summary>The live container's connection string, for tests that build their own connection.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public IDbConnection OpenConnection()
    {
        MySqlConnection connection = new(_container.GetConnectionString());
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using IDbConnection connection = OpenConnection();
        TestSchema.Apply(connection, TestSchema.StandardDdl);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
