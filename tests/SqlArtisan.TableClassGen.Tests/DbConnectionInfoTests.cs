using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// A refused connection needs no server, so the message the user actually sees on
// the commonest failure is testable without a container.
public class DbConnectionInfoTests
{
    [Fact]
    public void OpenConnection_Refused_NamesTheConnectionOptionsAndKeepsTheDriverMessage()
    {
        DbConnectionInfo info = new(
            Dbms.PostgreSql,
            host: "127.0.0.1",
            port: 1,
            serviceName: "db",
            schema: "public",
            username: "u",
            password: "p");

        CommandLineException error =
            Assert.Throws<CommandLineException>(() => info.OpenConnection());

        Assert.Contains("--host", error.Message, StringComparison.Ordinal);
        Assert.Contains("SQLARTISAN_DB_PASSWORD", error.Message, StringComparison.Ordinal);

        // The driver's own text carries the detail a rewritten message would drop,
        // and is what a user searches with.
        Assert.Contains("127.0.0.1:1", error.Message, StringComparison.Ordinal);
        Assert.Contains("The driver reported:", error.Message, StringComparison.Ordinal);
    }

    // Naming a DBMS the tool's own parser would reject is the bug DbmsOption records;
    // the message stays clear of engine spellings altogether.
    [Fact]
    public void OpenConnection_Refused_DoesNotSpellTheEngineName()
    {
        DbConnectionInfo info = new(
            Dbms.PostgreSql,
            host: "127.0.0.1",
            port: 1,
            serviceName: "db",
            schema: "public",
            username: "u",
            password: "p");

        CommandLineException error =
            Assert.Throws<CommandLineException>(() => info.OpenConnection());

        Assert.DoesNotContain("PostgreSql", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyCatalogMessage_Sqlite_NamesTheFileOptionAndTheCreatedFileTrap()
    {
        DbConnectionInfo info = new(
            Dbms.Sqlite,
            host: string.Empty,
            port: 0,
            serviceName: "/tmp/missing.db",
            schema: string.Empty,
            username: string.Empty,
            password: string.Empty);

        Assert.Contains("--service-name", info.EmptyCatalogMessage, StringComparison.Ordinal);
        Assert.Contains("/tmp/missing.db", info.EmptyCatalogMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyCatalogMessage_SchemaEngine_NamesTheSchemaOption()
    {
        DbConnectionInfo info = new(
            Dbms.PostgreSql,
            host: "localhost",
            port: 5432,
            serviceName: "db",
            schema: "typo",
            username: "u",
            password: "p");

        Assert.Contains("--schema", info.EmptyCatalogMessage, StringComparison.Ordinal);
        Assert.Contains("'typo'", info.EmptyCatalogMessage, StringComparison.Ordinal);
    }
}
