using System.Data;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// Oracle fixture on XE 21c (<c>gvenzl/oracle-xe:21.3.0-slim-faststart</c>), pinned with an
/// explicit <c>.WithImage()</c> like the other fixtures so the verified-against version never
/// tracks the module's default; the heaviest lane in the matrix.
/// </summary>
public sealed class OracleFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly OracleContainer _container = new OracleBuilder()
        .WithImage("gvenzl/oracle-xe:21.3.0-slim-faststart")
        .Build();

    public Dbms Dbms => Dbms.Oracle;

    /// <summary>The live container's connection string, for tests that build their own
    /// connection.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public IDbConnection OpenConnection()
    {
        OracleConnection connection = new(_container.GetConnectionString());
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using IDbConnection connection = OpenConnection();
        TestSchema.Apply(connection, TestSchema.OracleDdl);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
