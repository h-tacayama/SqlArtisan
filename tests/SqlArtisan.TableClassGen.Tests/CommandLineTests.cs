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

    // A blank --schema (a config file's "schema": "" included) must engage
    // the --database/--user fallback like a missing one — treating it as
    // present read zero tables silently.
    [Fact]
    public void Parse_BlankSchema_FallsBackToDatabaseOnMySql()
    {
        RunOptions options = CommandLine.Parse(
        [
            "--dbms", "mysql", "--host", "h", "--database", "appdb",
            "--user", "u", "--namespace", "N", "--schema", "",
        ]);

        Assert.Equal("appdb", options.Connection.Schema);
    }

    [Fact]
    public void Parse_BlankSchema_FallsBackToUserOnOracle()
    {
        RunOptions options = CommandLine.Parse(
        [
            "--dbms", "oracle", "--host", "h", "--database", "XEPDB1",
            "--user", "scott", "--namespace", "N", "--schema", " ",
        ]);

        Assert.Equal("scott", options.Connection.Schema);
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

    // Pinned as documented (README § "Options"): names ignore case and hyphens, in
    // flags and --config keys alike, and narrowing that later would break callers.
    [Fact]
    public void Parse_OptionNames_IgnoreCaseAndHyphens()
    {
        RunOptions options = CommandLine.Parse(
        [
            "--DBMS", "sqlite", "--File", "app.db", "--name-space", "N",
            "--DRYRUN", "--q-u-a-l-i-f-y-schema",
        ]);

        Assert.Equal(Dbms.Sqlite, options.Connection.Dbms);
        Assert.Equal("N", options.Settings.OutputNamespace);
        Assert.True(options.DryRun);
        Assert.True(options.Settings.QualifySchema);
    }

    [Fact]
    public void Parse_ConfigKeys_IgnoreCaseAndHyphens()
    {
        using TempFile config = TempFile.Create(
            """{"DBMS": "sqlite", "file": "app.db", "Namespace": "N", "dry-run": true}""");

        RunOptions options = CommandLine.Parse(["--config", config.Path, "--QualifySchema"]);

        Assert.Equal("N", options.Settings.OutputNamespace);
        Assert.True(options.DryRun);
        Assert.True(options.Settings.QualifySchema);
    }

    // Pinned as documented (README § "Options"): a relative path in the file is taken
    // as written, so it resolves against the working directory, never the file's own.
    [Fact]
    public void Parse_ConfigRelativePaths_ResolveAgainstTheWorkingDirectory()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "app.db", "namespace": "N", "output": "Gen"}""");

        RunOptions options = CommandLine.Parse(["--config", config.Path]);

        Assert.Equal("app.db", options.Connection.ServiceName);
        Assert.Equal("Gen", options.Settings.OutputDirectory);
    }

    [Fact]
    public void Parse_UnknownOption_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--tabels", "orders"]));

        Assert.Equal("Unknown option '--tabels' (see --help)", ex.Message);
    }

    // Last-wins narrowed `--check --tables b --tables a` to a, reporting in sync
    // over b's drift (#643).
    [Fact]
    public void Parse_RepeatedOption_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, "--tables", "b", "--tables", "a"]));

        Assert.Equal("'--tables' is given more than once (see --help)", ex.Message);
    }

    // A switch repeats harmlessly, but one rule for every option is the one a
    // caller can predict; two spellings of one option are a repeat as well.
    [Theory]
    [InlineData("--verbose", "--verbose")]
    [InlineData("--dry-run", "--dryrun")]
    public void Parse_RepeatedSwitch_ThrowsCommandLineException(string first, string second)
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse([.. MinimalSqlite, first, second]));

        Assert.Equal($"'{second}' is given more than once (see --help)", ex.Message);
    }

    [Fact]
    public void Parse_NonNumericPort_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(
                ["--dbms", "postgresql", "--host", "h", "--database", "d", "--schema", "s",
                 "--user", "u", "--namespace", "N", "--port", "54x2"]));

        Assert.Equal("--port must be a number between 1 and 65535 (got '54x2')", ex.Message);
    }

    // The parse path validates the same domain the interactive prompt does —
    // silently connecting to a nonsense port is the misconfiguration nobody sees.
    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("65536")]
    public void Parse_PortOutsideRange_ThrowsCommandLineException(string port)
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(
                ["--dbms", "postgresql", "--host", "h", "--database", "d", "--schema", "s",
                 "--user", "u", "--namespace", "N", "--port", port]));

        Assert.Equal($"--port must be a number between 1 and 65535 (got '{port}')", ex.Message);
    }

    // Blank counts as missing: --namespace "" previously emitted `namespace ;`.
    [Fact]
    public void Parse_BlankRequiredOption_ThrowsCommandLineException()
    {
        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(
                ["--dbms", "sqlite", "--file", "app.db", "--namespace", " "]));

        Assert.Equal(
            "--namespace is required (or set \"namespace\" in the --config file)",
            ex.Message);
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
            "--dbms must be one of mysql, oracle, postgresql (or postgres), sqlite, "
                + "sqlserver (or mssql) (got 'db2')",
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

    [Theory]
    [InlineData("""{"dbms":"sqlite","file":"a","namespace":"A","namespace":"B"}""", "namespace")]
    [InlineData("""{"dbms":"sqlite","file":"a","namespace":"A","Namespace":"B"}""", "Namespace")]
    public void Parse_RepeatedConfigKey_ThrowsCommandLineException(string json, string key)
    {
        using TempFile config = TempFile.Create(json);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal($"Key '{key}' is given more than once in {config.Path}", ex.Message);
    }

    // "$schema" is editor plumbing every JSON config file is entitled to carry.
    [Fact]
    public void Parse_ConfigFile_IgnoresDollarPrefixedKeys()
    {
        using TempFile config = TempFile.Create(
            """{"$schema": "https://example/schema.json", "dbms": "sqlite", "file": "a.db", "namespace": "N"}""");

        Assert.Equal("N", CommandLine.Parse(["--config", config.Path]).Settings.OutputNamespace);
    }

    // The option surface is comma-separated strings, so a comma-bearing array
    // element would silently split into two names.
    [Fact]
    public void Parse_ConfigTablesElementWithComma_ThrowsCommandLineException()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "a.db", "namespace": "N", "tables": ["weird,name"]}""");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal("\"tables\" array elements must not contain commas", ex.Message);
    }

    [Fact]
    public void Parse_MalformedConfigJson_ThrowsCommandLineException()
    {
        using TempFile config = TempFile.Create("{ not valid json");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.StartsWith($"--config file is not valid JSON: {config.Path} (", ex.Message);
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

    // JSON null is "not set", like an absent key; it must not turn a switch on.
    [Fact]
    public void Parse_ConfigNullFlag_ReadsAsUnset()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "a.db", "namespace": "N", "dry-run": null, "verbose": null}""");

        RunOptions options = CommandLine.Parse(["--config", config.Path]);

        Assert.False(options.DryRun);
        Assert.False(options.Verbose);
    }

    [Fact]
    public void Parse_FormatJson_SetsJson()
    {
        RunOptions options = CommandLine.Parse(
            ["--dbms", "sqlite", "--file", "a.db", "--namespace", "N", "--format", "json"]);

        Assert.True(options.Json);
    }

    // A switch is on or off: a number or a word read as "on" would run --fix.
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("\"no\"")]
    [InlineData("\"yes\"")]
    public void Parse_ConfigNonBooleanSwitch_ThrowsCommandLineException(string json)
    {
        using TempFile config = TempFile.Create(
            $$"""{"dbms": "sqlite", "file": "a.db", "namespace": "N", "fix": {{json}}}""");

        CommandLineException ex = Assert.Throws<CommandLineException>(() =>
            CommandLine.Parse(["--config", config.Path]));

        Assert.Equal(
            $"\"fix\" in the --config file must be true or false (got '{json.Trim('"')}')",
            ex.Message);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("\"true\"", true)]
    [InlineData("false", false)]
    [InlineData("\"False\"", false)]
    public void Parse_ConfigBooleanSwitch_ReadsTheSwitch(string json, bool expected)
    {
        using TempFile config = TempFile.Create(
            $$"""{"dbms": "sqlite", "file": "a.db", "namespace": "N", "dry-run": {{json}}}""");

        RunOptions options = CommandLine.Parse(["--config", config.Path]);

        Assert.Equal(expected, options.DryRun);
    }

    [Fact]
    public void Parse_ConfigNullRequiredKey_ThrowsCommandLineException()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "a.db", "namespace": null}""");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal(
            "--namespace is required (or set \"namespace\" in the --config file)",
            ex.Message);
    }

    [Fact]
    public void Parse_ConfigTablesNullElement_ThrowsCommandLineException()
    {
        using TempFile config = TempFile.Create(
            """{"dbms": "sqlite", "file": "a.db", "namespace": "N", "tables": ["orders", null]}""");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal("\"tables\" array elements must not be null", ex.Message);
    }

    [Theory]
    [InlineData("postgres", Dbms.PostgreSql)]
    [InlineData("mssql", Dbms.SqlServer)]
    public void Parse_DbmsAlias_ResolvesTheEngine(string alias, Dbms expected)
    {
        RunOptions options = CommandLine.Parse(
            ["--dbms", alias, "--host", "h", "--database", "d", "--schema", "s", "--user", "u",
             "--namespace", "N"]);

        Assert.Equal(expected, options.Connection.Dbms);
    }

    // Every key: JsonElement.ToString() on an object is its raw JSON, which the
    // blank check downstream accepts (release audit pass 8).
    [Theory]
    [InlineData("namespace", "a string or number")]
    [InlineData("host", "a string or number")]
    [InlineData("database", "a string or number")]
    [InlineData("user", "a string or number")]
    [InlineData("schema", "a string or number")]
    [InlineData("output", "a string or number")]
    [InlineData("file", "a string or number")]
    [InlineData("tables", "a string or an array of strings")]
    [InlineData("port", "a string or number")]
    [InlineData("fix", "true or false")]
    public void Parse_ConfigObjectValue_ThrowsCommandLineException(string key, string expected)
    {
        using TempFile config = ConfigWith(key, "{\"nested\": \"x\"}");

        CommandLineException ex = Assert.Throws<CommandLineException>(() =>
            CommandLine.Parse(["--config", config.Path]));

        Assert.Equal(
            $"\"{key}\" in the --config file must be {expected} (got an object)",
            ex.Message);
    }

    // Each option takes the kinds its command-line form can spell; read as text,
    // "output": false generated into a directory named false.
    [Theory]
    [InlineData("output", "false", "a string or number (got a boolean)")]
    [InlineData("namespace", "[\"N\"]", "a string or number (got an array)")]
    [InlineData("tables", "true", "a string or an array of strings (got a boolean)")]
    [InlineData("tables", "5", "a string or an array of strings (got a number)")]
    [InlineData("fix", "[true]", "true or false (got an array)")]
    public void Parse_ConfigValueOfTheWrongKind_ThrowsCommandLineException(
        string key,
        string json,
        string expected)
    {
        using TempFile config = ConfigWith(key, json);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal($"\"{key}\" in the --config file must be {expected}", ex.Message);
    }

    [Fact]
    public void Parse_ConfigTablesNonStringElement_ThrowsCommandLineException()
    {
        using TempFile config = ConfigWith("tables", "[\"orders\", 5]");

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal("\"tables\" array elements must be strings", ex.Message);
    }

    // EnumerateObject on another root threw InvalidOperationException, whose
    // message named neither --config nor the file.
    [Theory]
    [InlineData("[\"dbms\"]", "an array")]
    [InlineData("\"sqlite\"", "a string")]
    [InlineData("null", "null")]
    public void Parse_ConfigRootNotAnObject_ThrowsCommandLineException(
        string json,
        string kind)
    {
        using TempFile config = TempFile.Create(json);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.Equal(
            $"--config file must hold a JSON object of options: {config.Path} (got {kind})",
            ex.Message);
    }

    // File.ReadAllText's exception reached stderr naming neither --config nor
    // what to fix; a missing file was already reported against the flag.
    [Fact]
    public void Parse_UnreadableConfig_ThrowsCommandLineException()
    {
        // File modes do not bind root, and Windows has none.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        using TempFile config = TempFile.Create("{}");
        File.SetUnixFileMode(config.Path, UnixFileMode.None);

        CommandLineException ex = Assert.Throws<CommandLineException>(
            () => CommandLine.Parse(["--config", config.Path]));

        Assert.StartsWith(
            $"--config file cannot be read: {config.Path} (",
            ex.Message,
            StringComparison.Ordinal);
    }

    // A blank value is an absent one at every source, so a blank flag leaves the
    // key to the --config file and a blank anything reads its default.
    [Fact]
    public void Parse_BlankValues_ReadAsAbsent()
    {
        using TempFile config = ConfigWith("tables", "[]");

        RunOptions options = CommandLine.Parse(
            ["--config", config.Path, "--output", " ", "--format", "", "--accessibility", ""]);

        Assert.Equal(".", options.Settings.OutputDirectory);
        Assert.Empty(options.Settings.TableNames);
        Assert.False(options.Json);
        Assert.Equal("internal", options.Settings.Accessibility);
    }

    [Fact]
    public void Parse_BlankFlag_LeavesTheKeyToTheConfigFile()
    {
        using TempFile config = ConfigWith("output", "\"src/Tables\"");

        RunOptions options = CommandLine.Parse(["--config", config.Path, "--output", ""]);

        Assert.Equal("src/Tables", options.Settings.OutputDirectory);
    }

    // The key under test replaces its base entry: a repeated key is rejected before
    // its value's kind is read.
    private static TempFile ConfigWith(string key, string json)
    {
        string baseKeys = string.Join(
            ", ",
            new[] { "\"dbms\": \"sqlite\"", "\"file\": \"a.db\"", "\"namespace\": \"N\"" }
                .Where(entry => !entry.StartsWith($"\"{key}\"", StringComparison.Ordinal)));

        return TempFile.Create($"{{{baseKeys}, \"{key}\": {json}}}");
    }
}
