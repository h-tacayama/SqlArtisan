using System.Text.Json;

namespace SqlArtisan.TableClassGen;

// Every message names the flag (and its config-file key) to change, because the
// caller correcting it is often a script or an agent reading only stderr.
internal sealed class CommandLineException(string message) : Exception(message);

internal static class CommandLine
{
    public const string PasswordEnvironmentVariable = "SQLARTISAN_DB_PASSWORD";

    // Both sets are stored normalized, because that is how every lookup arrives —
    // spelling them with hyphens here silently broke --dry-run and --qualify-schema.
    private static readonly HashSet<string> Switches =
    [
        .. new[] { "check", "fix", "dry-run", "verbose", "lowercase", "subfolders", "qualify-schema" }
            .Select(Normalize),
    ];

    private static readonly HashSet<string> KnownOptions =
    [
        .. new[]
        {
            "config", "dbms", "host", "port", "database", "schema", "user", "file",
            "namespace", "output", "tables", "accessibility", "format",
        }.Select(Normalize).Concat(Switches),
    ];

    public static string HelpText =>
        $"""
        sa-tableclassgen — generate SqlArtisan table classes from a live database.

        Usage:
          sa-tableclassgen [options]            generate table classes
          sa-tableclassgen --check [options]    report drift, write nothing
          sa-tableclassgen --fix [options]      regenerate only the tables that drifted
          sa-tableclassgen                      interactive prompts (terminal only)

        Connection:
          --dbms <name>          mysql | oracle | postgresql | sqlite | sqlserver
          --host <host>          database host
          --port <n>             database port (defaults per DBMS)
          --database <name>      database or Oracle service name
          --schema <name>        schema to read (PostgreSQL, SQL Server, Oracle)
          --user <name>          user name
          --file <path>          SQLite database file
          The password is read from the {PasswordEnvironmentVariable} environment
          variable; there is no password flag.

        Output:
          --namespace <ns>       namespace of the generated classes
          --output <dir>         output directory
          --tables <a,b,c>       act on these tables only (default: every table)
          --accessibility <a>    internal (default) or public
          --qualify-schema       emit schema-qualified table names
          --lowercase            lowercase the names taken from the catalog
          --subfolders           write into subfolders by class-name initial

        General:
          --config <path>        JSON file of the options above; flags win
          --dry-run              report what would be written, write nothing
          --format <text|json>   output format (default text)
          --verbose              report every file, not just the summary
          --help                 show this help

        Exit codes: 0 success or in sync, 1 drift, 2 error.
        """;

    public static bool WantsHelp(string[] args) =>
        args.Any(a => a is "--help" or "-h" or "-?");

    public static RunOptions Parse(string[] args)
    {
        Dictionary<string, string> values = ParseArguments(args);

        if (values.TryGetValue("config", out string? configPath))
        {
            foreach (KeyValuePair<string, string> entry in ReadConfigFile(configPath))
            {
                // Flags win over the file, so the file only fills what was not given.
                if (!values.ContainsKey(entry.Key))
                {
                    values[entry.Key] = entry.Value;
                }
            }
        }

        return Build(values);
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CommandLineException(
                    $"Unexpected argument '{argument}' (options start with '--'; see --help)");
            }

            string name = Normalize(argument[2..]);

            // An unrecognized option is rejected rather than ignored: a typo'd
            // --tabels would otherwise generate every table without a word.
            if (!KnownOptions.Contains(name))
            {
                throw new CommandLineException($"Unknown option '{argument}' (see --help)");
            }

            if (Switches.Contains(name))
            {
                values[name] = "true";
                continue;
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new CommandLineException($"'{argument}' requires a value (see --help)");
            }

            values[name] = args[++i];
        }

        return values;
    }

    // Keys are compared with separators removed, so --qualify-schema and a
    // "qualifySchema" config property are the same option.
    private static string Normalize(string key) =>
        key.Replace("-", string.Empty).ToLowerInvariant();

    private static Dictionary<string, string> ReadConfigFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new CommandLineException($"--config file not found: {path}");
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal);

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(path),
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                // "$schema" and friends are editor plumbing, not options.
                if (property.Name.StartsWith("$", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!KnownOptions.Contains(Normalize(property.Name)))
                {
                    throw new CommandLineException(
                        $"Unknown key '{property.Name}' in {path} (see --help)");
                }

                values[Normalize(property.Name)] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Array => string.Join(
                        ",",
                        property.Value.EnumerateArray().Select(e => e.ToString())),
                    _ => property.Value.ToString(),
                };
            }
        }
        catch (JsonException ex)
        {
            throw new CommandLineException($"--config file is not valid JSON: {path} ({ex.Message})");
        }

        return values;
    }

    private static RunOptions Build(Dictionary<string, string> values)
    {
        bool check = Flag(values, "check");
        bool fix = Flag(values, "fix");

        if (check && fix)
        {
            throw new CommandLineException("--check and --fix cannot be combined");
        }

        string format = Value(values, "format") ?? "text";

        if (format is not ("text" or "json"))
        {
            throw new CommandLineException($"--format must be text or json (got '{format}')");
        }

        DbmsType dbms = ParseDbms(Required(values, "dbms"));

        return new RunOptions(
            check ? RunMode.Check : fix ? RunMode.Fix : RunMode.Generate,
            BuildConnection(values, dbms),
            BuildSettings(values),
            dryRun: Flag(values, "dry-run"),
            json: format == "json",
            verbose: Flag(values, "verbose"));
    }

    private static DbConnectionInfo BuildConnection(
        Dictionary<string, string> values, DbmsType dbms)
    {
        if (dbms == DbmsType.Sqlite)
        {
            return new DbConnectionInfo(
                dbms, string.Empty, 0, Required(values, "file"), string.Empty,
                string.Empty, string.Empty);
        }

        string database = Required(values, "database");
        string user = Required(values, "user");

        return new DbConnectionInfo(
            dbms,
            Required(values, "host"),
            ResolvePort(values, dbms),
            database,
            ResolveSchema(values, dbms, database, user),
            user,
            Environment.GetEnvironmentVariable(PasswordEnvironmentVariable) ?? string.Empty);
    }

    // An unparseable port is an error rather than a fallback to the default: silently
    // connecting to another port is the kind of misconfiguration nobody sees.
    private static int ResolvePort(Dictionary<string, string> values, DbmsType dbms)
    {
        if (Value(values, "port") is not { } port)
        {
            return DefaultPort(dbms);
        }

        return int.TryParse(port, out int parsed)
            ? parsed
            : throw new CommandLineException($"--port must be a number (got '{port}')");
    }

    // MySQL has no schema layer above the database, and Oracle's schema is the user
    // unless one is named, so neither makes --schema mandatory.
    private static string ResolveSchema(
        Dictionary<string, string> values, DbmsType dbms, string database, string user) =>
        dbms switch
        {
            DbmsType.MySql => Value(values, "schema") ?? database,
            DbmsType.Oracle => Value(values, "schema") ?? user,
            _ => Required(values, "schema"),
        };

    private static CodeGenerationSettings BuildSettings(Dictionary<string, string> values)
    {
        string accessibility = Value(values, "accessibility") ?? "internal";

        if (accessibility is not ("internal" or "public"))
        {
            throw new CommandLineException(
                $"--accessibility must be internal or public (got '{accessibility}')");
        }

        return new CodeGenerationSettings(
            Required(values, "namespace"),
            Flag(values, "lowercase"),
            Value(values, "output") ?? ".",
            Flag(values, "subfolders"),
            SplitTables(Value(values, "tables")),
            accessibility,
            Flag(values, "qualify-schema"));
    }

    private static IReadOnlyList<string> SplitTables(string? tables) =>
        string.IsNullOrWhiteSpace(tables)
            ? []
            : [.. tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static DbmsType ParseDbms(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "mysql" => DbmsType.MySql,
            "oracle" => DbmsType.Oracle,
            "postgresql" or "postgres" => DbmsType.PostgreSql,
            "sqlite" => DbmsType.Sqlite,
            "sqlserver" or "mssql" => DbmsType.SqlServer,
            _ => throw new CommandLineException(
                $"--dbms must be one of mysql, oracle, postgresql, sqlite, sqlserver (got '{value}')"),
        };

    private static int DefaultPort(DbmsType dbms) =>
        dbms switch
        {
            DbmsType.Oracle => 1521,
            DbmsType.PostgreSql => 5432,
            DbmsType.MySql => 3306,
            DbmsType.SqlServer => 1433,
            _ => 0,
        };

    private static string? Value(Dictionary<string, string> values, string key) =>
        values.TryGetValue(Normalize(key), out string? value) ? value : null;

    private static string Required(Dictionary<string, string> values, string key) =>
        Value(values, key)
            ?? throw new CommandLineException(
                $"--{key} is required (or set \"{key}\" in the --config file)");

    private static bool Flag(Dictionary<string, string> values, string key) =>
        Value(values, key) is { } value
        && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
