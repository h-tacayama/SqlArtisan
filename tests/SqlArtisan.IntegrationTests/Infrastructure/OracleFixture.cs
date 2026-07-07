using System.Data;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// Oracle fixture, backed by Oracle Database XE 21c
/// (<c>gvenzl/oracle-xe:21.3.0-slim-faststart</c>). The tag is pinned
/// explicitly — matching the other fixtures — so the verified-against version
/// is deterministic rather than tracking the Testcontainers module's default,
/// which has drifted across releases. The image is large and slow to start, so
/// the Oracle lane is the heaviest entry in the matrix.
/// </summary>
public sealed class OracleFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly OracleContainer _container = new OracleBuilder()
        .WithImage("gvenzl/oracle-xe:21.3.0-slim-faststart")
        .Build();

    public Dbms Dbms => Dbms.Oracle;

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
