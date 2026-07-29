using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// A refused connection needs no server, so the message the user actually sees on
// the commonest failure is testable without a container.
public class DbConnectionInfoTests
{
    private static DbConnectionInfo Refused() =>
        new(
            Dbms.PostgreSql,
            host: "127.0.0.1",
            port: 1,
            serviceName: "db",
            schema: "public",
            username: "u",
            password: "p");

    [Fact]
    public void OpenConnection_Refused_NamesOptionsTheCommandLineAccepts()
    {
        CommandLineException error =
            Assert.Throws<CommandLineException>(() => Refused().OpenConnection());

        Assert.Contains("--host", error.Message, StringComparison.Ordinal);
        Assert.Contains("--database", error.Message, StringComparison.Ordinal);
        Assert.Contains("--user,", error.Message, StringComparison.Ordinal);
        Assert.Contains("SQLARTISAN_DB_PASSWORD", error.Message, StringComparison.Ordinal);
    }

    // The bug this pins: an earlier version named --service-name and --username,
    // which are DbConnectionInfo's constructor parameters, not options the parser
    // accepts — so following the advice produced "Unknown option".
    [Theory]
    [InlineData("--service-name")]
    [InlineData("--username")]
    public void OpenConnection_Refused_NamesNoOptionTheCommandLineWouldReject(string absent)
    {
        CommandLineException error =
            Assert.Throws<CommandLineException>(() => Refused().OpenConnection());

        Assert.DoesNotContain(absent, error.Message, StringComparison.Ordinal);
    }

    // Asserting the driver's own words would pin a message that changes with the
    // provider, so what is pinned is that something survives the marker.
    [Fact]
    public void OpenConnection_Refused_KeepsTheDriverMessageAsTheCause()
    {
        const string Marker = "The driver reported: ";

        CommandLineException error =
            Assert.Throws<CommandLineException>(() => Refused().OpenConnection());

        int at = error.Message.IndexOf(Marker, StringComparison.Ordinal);

        Assert.True(at >= 0, $"no '{Marker}' in: {error.Message}");
        Assert.NotEmpty(error.Message[(at + Marker.Length)..].Trim());
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

        Assert.Contains("--file", info.EmptyCatalogMessage, StringComparison.Ordinal);
        Assert.Contains("/tmp/missing.db", info.EmptyCatalogMessage, StringComparison.Ordinal);
    }

    // --schema is optional on these two, so the option it falls back to is the one
    // the user can actually have got wrong.
    [Theory]
    [InlineData(Dbms.MySql, "--database")]
    [InlineData(Dbms.Oracle, "--user")]
    public void EmptyCatalogMessage_SchemaDefaultingEngine_NamesWhatItDefaultsTo(
        Dbms dbms,
        string expected)
    {
        DbConnectionInfo info = new(
            dbms,
            host: "localhost",
            port: 1521,
            serviceName: "db",
            schema: "typo",
            username: "u",
            password: "p");

        Assert.Contains("--schema", info.EmptyCatalogMessage, StringComparison.Ordinal);
        Assert.Contains(expected, info.EmptyCatalogMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyCatalogMessage_RequiredSchemaEngine_NamesTheSchemaOption()
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
