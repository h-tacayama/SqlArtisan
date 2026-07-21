using System.Data;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// A second, newer Oracle fixture used only by <c>Oracle23aiBoundSweepTests</c>
/// to live-prove the analyzer's Oracle version bounds (#263) — the ordinary
/// <see cref="OracleFixture"/> lane stays on the pinned 21c baseline
/// (<see cref="DialectMatrix.VerifiedAgainstVersion"/>) so its matrix-bool
/// expectations (e.g. WITH RECURSIVE rejected) are unaffected.
/// </summary>
public sealed class Oracle23aiFixture : IAsyncLifetime, IDatabaseFixture
{
    private readonly OracleContainer _container = new OracleBuilder()
        .WithImage("gvenzl/oracle-free:23-slim-faststart")
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
