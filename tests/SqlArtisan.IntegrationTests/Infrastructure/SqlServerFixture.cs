using System.Data;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>SQL Server fixture, backed by a Testcontainers <c>mssql/server</c> container.</summary>
public sealed class SqlServerFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Dbms Dbms => Dbms.SqlServer;

    /// <summary>The live container's connection string, for tests that build their own connection.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public IDbConnection OpenConnection()
    {
        SqlConnection connection = new(_container.GetConnectionString());
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using IDbConnection connection = OpenConnection();
        TestSchema.Apply(connection, TestSchema.SqlServerDdl);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
