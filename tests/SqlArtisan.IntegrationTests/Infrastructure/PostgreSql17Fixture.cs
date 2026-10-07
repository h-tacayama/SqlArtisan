using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// The PostgreSQL 17 lane's fixture, live-proving the analyzer's PostgreSQL version bounds
/// above the pinned 16 baseline; the ordinary <see cref="PostgreSqlFixture"/> lane stays on
/// 16 (<see cref="DialectMatrix.VerifiedAgainstVersion"/>) so its matrix bools hold.
/// </summary>
public sealed class PostgreSql17Fixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public Dbms Dbms => Dbms.PostgreSql;

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
