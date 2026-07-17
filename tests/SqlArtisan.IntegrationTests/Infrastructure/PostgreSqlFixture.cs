using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>PostgreSQL fixture, backed by a Testcontainers <c>postgres</c> container.</summary>
public sealed class PostgreSqlFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Dbms Dbms => Dbms.PostgreSql;

    /// <summary>The live container's connection string, for tests that build their own connection.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public IDbConnection OpenConnection()
    {
        NpgsqlConnection connection = new(_container.GetConnectionString());
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using IDbConnection connection = OpenConnection();
        TestSchema.Apply(connection, TestSchema.PostgreSqlDdl);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
