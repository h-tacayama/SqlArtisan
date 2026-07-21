using System.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// A second, newer Oracle fixture used only by <c>Oracle23aiBoundSweepTests</c>
/// to live-prove the analyzer's Oracle version bounds (#263) — the ordinary
/// <see cref="OracleFixture"/> lane stays on the pinned 21c baseline
/// (<see cref="DialectMatrix.VerifiedAgainstVersion"/>) so its matrix-bool
/// expectations (e.g. WITH RECURSIVE rejected) are unaffected.
/// </summary>
/// <remarks>
/// Built from the generic <see cref="ContainerBuilder"/> rather than
/// <c>Testcontainers.Oracle.OracleBuilder</c>: that builder's connection
/// string hardcodes the service name to <c>XEPDB1</c> (the <c>oracle-xe</c>
/// image's pluggable database) with no public way to override it, and
/// <c>gvenzl/oracle-free</c>'s default pluggable database is <c>FREEPDB1</c>
/// instead — using the typed builder against this image connects to a
/// service name that does not exist (ORA-12514, confirmed live in CI).
/// </remarks>
public sealed class Oracle23aiFixture : IAsyncLifetime, IDatabaseFixture
{
    private const string Username = "oracle";
    private const string Password = "oracle";
    private const string Database = "FREEPDB1";

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("gvenzl/oracle-free:23-slim-faststart")
        .WithPortBinding(1521, assignRandomHostPort: true)
        .WithEnvironment("ORACLE_PASSWORD", Password)
        .WithEnvironment("APP_USER", Username)
        .WithEnvironment("APP_USER_PASSWORD", Password)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("DATABASE IS READY TO USE!"))
        .Build();

    public Dbms Dbms => Dbms.Oracle;

    public IDbConnection OpenConnection()
    {
        string connectionString =
            $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={_container.Hostname})"
                + $"(PORT={_container.GetMappedPublicPort(1521)}))(CONNECT_DATA=(SERVICE_NAME={Database})));"
                + $"User Id={Username};Password={Password};";
        OracleConnection connection = new(connectionString);
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
