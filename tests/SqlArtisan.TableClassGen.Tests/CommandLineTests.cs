using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class CommandLineTests
{
    private static readonly string[] MinimalSqlite =
        ["--dbms", "sqlite", "--file", "app.db", "--namespace", "MyApp.Tables"];

    [Fact]
    public void Parse_MinimalArguments_DefaultsToGenerate()
    {
        RunOptions options = CommandLine.Parse(MinimalSqlite);

        Assert.Equal(RunMode.Generate, options.Mode);
        Assert.Equal(Dbms.Sqlite, options.Connection.Dbms);
        Assert.Equal("app.db", options.Connection.ServiceName);
        Assert.Equal("MyApp.Tables", options.Settings.OutputNamespace);
        Assert.Empty(options.Settings.TableNames);
        Assert.False(options.Json);
    }

    [Fact]
    public void Parse_Check_SelectsCheckMode()
    {
        Assert.Equal(RunMode.Check, CommandLine.Parse([.. MinimalSqlite, "--check"]).Mode);
    }

    [Fact]
    public void Parse_CheckAndFix_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--check", "--fix"]));

        Assert.Equal("--check and --fix cannot be combined", ex.Message);
    }

    // The switch set is matched against hyphen-stripped names, so spelling it with
    // hyphens made these two demand a value instead of standing alone.
    [Fact]
    public void Parse_HyphenatedSwitches_StandAlone()
    {
        RunOptions options = CommandLine.Parse([.. MinimalSqlite, "--dry-run", "--qualify-schema"]);

        Assert.True(options.DryRun);
        Assert.True(options.Settings.QualifySchema);
    }

    [Fact]
    public void Parse_UnknownOption_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--tabels", "orders"]));

        Assert.Equal("Unknown option '--tabels' (see --help)", ex.Message);
    }

    [Fact]
    public void Parse_NonNumericPort_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(
                ["--dbms", "postgresql", "--host", "h", "--database", "d", "--schema", "s",
                 "--user", "u", "--namespace", "N", "--port", "54x2"]));

        Assert.Equal("--port must be a number (got '54x2')", ex.Message);
    }

    [Fact]
    public void Parse_Tables_SplitsAndTrims()
    {
        RunOptions options = CommandLine.Parse([.. MinimalSqlite, "--tables", "a, b ,c"]);

        Assert.Equal(["a", "b", "c"], options.Settings.TableNames);
    }

    // The message has to name what to change: the caller correcting it is often a
    // script reading only stderr.
    [Fact]
    public void Parse_MissingRequiredOption_NamesTheFlagAndConfigKey()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--dbms", "sqlite", "--file", "app.db"]));

        Assert.Equal(
            "--namespace is required (or set \"namespace\" in the --config file)",
            ex.Message);
    }

    [Fact]
    public void Parse_UnknownDbms_ListsTheAcceptedValues()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--dbms", "db2", "--namespace", "N"]));

        Assert.Equal(
            "--dbms must be one of mysql, oracle, postgresql, sqlite, sqlserver (got 'db2')",
            ex.Message);
    }

    [Fact]
    public void Parse_OptionWithoutValue_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--dbms", "sqlite", "--namespace"]));

        Assert.Equal("'--namespace' requires a value (see --help)", ex.Message);
    }

    [Fact]
    public void Parse_BareArgument_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["generate"]));

        Assert.Equal(
            "Unexpected argument 'generate' (options start with '--'; see --help)",
            ex.Message);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--format", "xml"]));

        Assert.Equal("--format must be text or json (got 'xml')", ex.Message);
    }

    [Fact]
    public void Parse_InvalidAccessibility_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--accessibility", "private"]));

        Assert.Equal("--accessibility must be internal or public (got 'private')", ex.Message);
    }

    [Fact]
    public void Parse_ConfigFile_FillsUnsetOptions()
    {
        using TempFile config = TempFile.Create(
            """
            {
              "dbms": "sqlite",
              "file": "app.db",
              "namespace": "FromConfig",
              "qualifySchema": true,
              "tables": ["orders", "items"]
            }
            """);

        RunOptions options = CommandLine.Parse(["--config", config.Path]);

        Assert.Equal("FromConfig", options.Settings.OutputNamespace);
        Assert.True(options.Settings.QualifySchema);
        Assert.Equal(["orders", "items"], options.Settings.TableNames);
    }

    [Fact]
    public void Parse_ConfigFileAndFlag_FlagWins()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "app.db", "namespace": "FromConfig"}""");

        RunOptions options = CommandLine.Parse(
            ["--config", config.Path, "--namespace", "FromFlag"]);

        Assert.Equal("FromFlag", options.Settings.OutputNamespace);
    }

    [Fact]
    public void Parse_UnknownConfigKey_ThrowsCommandLineException()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "app.db", "namespace": "N", "namesapce": "typo"}""");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal($"Unknown key 'namesapce' in {config.Path} (see --help)", ex.Message);
    }

    // "$schema" is editor plumbing every JSON config file is entitled to carry.
    [Fact]
    public void Parse_ConfigFile_IgnoresDollarPrefixedKeys()
    {
        using TempFile config = TempFile.Create(
            """{"$schema": "https://example/schema.json", "dbms": "sqlite", "file": "a.db", "namespace": "N"}""");

        Assert.Equal("N", CommandLine.Parse(["--config", config.Path]).Settings.OutputNamespace);
    }

    [Fact]
    public void Parse_MissingConfigFile_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", "no-such-file.json"]));

        Assert.Equal("--config file not found: no-such-file.json", ex.Message);
    }

    [Fact]
    public void WantsHelp_RecognizesTheHelpFlags()
    {
        Assert.True(CommandLine.WantsHelp(["--help"]));
        Assert.True(CommandLine.WantsHelp(["--check", "-h"]));
        Assert.False(CommandLine.WantsHelp(["--check"]));
    }
}
