using System.Data;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// A live database the integration tests run against. Each implementation owns
/// the engine's lifecycle (a Testcontainers container, or an in-process file for
/// SQLite), creates the test schema, and seeds the baseline data — so a test
/// only needs an open connection of the right provider type. The provider type
/// is what <see cref="DbmsResolver"/> keys off, so executing a SqlArtisan
/// builder through the connection picks the correct dialect automatically.
/// </summary>
public interface IDatabaseFixture
{
    /// <summary>The engine behind this fixture, for documentation and trait filtering.</summary>
    Dbms Dbms { get; }

    /// <summary>Opens a fresh connection to the seeded database.</summary>
    IDbConnection OpenConnection();
}
