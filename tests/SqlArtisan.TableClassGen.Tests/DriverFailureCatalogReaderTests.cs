using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// The header read at connect passes a file whose stored schema is malformed; the
// first catalog query then fails, and used to reach stderr naming no --file.
public class DriverFailureCatalogReaderTests
{
    [Fact]
    public void GetAllTables_SqliteMalformedSchema_FailsNamingTheFileOption()
    {
        using TempSqliteDatabase db = MalformedSchema();

        CommandLineException error = Assert.Throws<CommandLineException>(
            () => Reader(db).GetAllTables());

        AssertNamesTheFile(db, error);
    }

    [Fact]
    public void TryGetTable_SqliteMalformedSchema_FailsNamingTheFileOption()
    {
        using TempSqliteDatabase db = MalformedSchema();

        CommandLineException error = Assert.Throws<CommandLineException>(
            () => Reader(db).TryGetTable("t", out _));

        AssertNamesTheFile(db, error);
    }

    // The tool's own guards already name what to change, so they pass through as thrown.
    [Fact]
    public void GetAllTables_CommandLineException_PassesThroughUnchanged()
    {
        CommandLineException thrown = new("No tables found in schema 's'; check --schema");

        CommandLineException error = Assert.Throws<CommandLineException>(
            () => Stubbed(thrown).GetAllTables());

        Assert.Same(thrown, error);
    }

    [Fact]
    public void GetAllTables_ToolBug_PassesThroughUnchanged()
    {
        InvalidOperationException thrown = new("bug");

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => Stubbed(thrown).GetAllTables());

        Assert.Same(thrown, error);
    }

    [Fact]
    public void CannotReadCatalogMessage_Server_NamesTheTargetAndNoFlag()
    {
        DbConnectionInfo info = new(
            Dbms.PostgreSql,
            host: "localhost",
            port: 5432,
            serviceName: "db",
            schema: "s",
            username: "u",
            password: "p");

        Assert.Equal(
            "Cannot read the catalog on localhost:5432 as 'u' after connecting",
            info.CannotReadCatalogMessage);
    }

    private static TempSqliteDatabase MalformedSchema()
    {
        TempSqliteDatabase db = TempSqliteDatabase.Create("CREATE TABLE t (id INTEGER);");
        db.Execute(
            """
            PRAGMA writable_schema = 1;
            UPDATE sqlite_master SET sql = 'CREATE TABLE t (id INTEGER' WHERE name = 't';
            """);

        return db;
    }

    private static ICatalogReader Reader(TempSqliteDatabase db) =>
        CatalogReaderFactory.Create(db.ConnectionInfo, lowercaseNames: false);

    private static ICatalogReader Stubbed(Exception thrown) =>
        new DriverFailureCatalogReader(new ThrowingCatalogReader(thrown), Server());

    private static DbConnectionInfo Server() =>
        new(Dbms.PostgreSql, "localhost", 5432, "db", "s", "u", "p");

    // Asserting the driver's own words would pin a message that changes with the
    // provider, so what is pinned is the tool's sentence and a cause appended to it.
    private static void AssertNamesTheFile(TempSqliteDatabase db, CommandLineException error)
    {
        string sentence =
            $"Cannot read the SQLite database file '{db.ConnectionInfo.ServiceName}'; "
                + "check --file (";

        Assert.StartsWith(sentence, error.Message, StringComparison.Ordinal);
        Assert.EndsWith(")", error.Message, StringComparison.Ordinal);
        Assert.NotEmpty(error.Message[sentence.Length..^1].Trim());
    }

    private sealed class ThrowingCatalogReader(Exception thrown) : ICatalogReader
    {
        public IReadOnlyList<CatalogTable> GetAllTables() => throw thrown;

        public bool TryGetTable(string tableName, out CatalogTable? table) => throw thrown;
    }
}
