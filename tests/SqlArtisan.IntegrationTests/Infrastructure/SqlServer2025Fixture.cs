using System.Data;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// The SQL Server 2025 lane's fixture, live-proving what 2025 added above the pinned 2022
/// baseline (<c>||</c> and the <c>REGEXP_*</c> family); the ordinary
/// <see cref="SqlServerFixture"/> lane stays on 2022 so its matrix bools hold.
/// </summary>
public sealed class SqlServer2025Fixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
        .Build();

    public Dbms Dbms => Dbms.SqlServer;

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
