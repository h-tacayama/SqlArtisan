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
        .. new[]
        {
            "check", "fix", "dry-run", "verbose", "lowercase", "subfolders", "qualify-schema",
        }
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
          sa-tableclassgen --fix [options]      regenerate the drifted tables, report them
          sa-tableclassgen                      interactive prompts (terminal only)

        Connection:
          --dbms <name>          mysql | oracle | postgresql (postgres) | sqlite | sqlserver (mssql)
          --host <host>          database host
          --port <n>             database port (defaults per DBMS)
          --database <name>      database or Oracle service name
          --schema <name>        schema to read: required on PostgreSQL and SQL Server;
                                 defaults to --database on MySQL, to --user on Oracle
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

    // Normalized like every other option name, so --HELP shows help rather than
    // failing as an unknown option.
    public static bool WantsHelp(string[] args) =>
        args.Any(a => a is "-h" or "-?"
            || (a.StartsWith("--", StringComparison.Ordinal) && Normalize(a[2..]) == "help"));

    public static RunOptions Parse(string[] args)
    {
        Dictionary<string, string> values = ParseArguments(args);

        if (Value(values, "config") is { } configPath)
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
        HashSet<string> given = new(StringComparer.Ordinal);

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

            // Last-wins would silently narrow a run: `--tables b --tables a` checked
            // only a, and --check reported in sync over b's drift (#643).
            if (!given.Add(name))
            {
                throw new CommandLineException(
                    $"'{argument}' is given more than once (see --help)");
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

            string value = args[++i];

            // Blank is absent, here and in ReadConfigFile: `--tables "$CHANGED"` with
            // nothing changed reads every table, and `--output ""` the default directory.
            if (!string.IsNullOrWhiteSpace(value))
            {
                values[name] = value;
            }
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
        HashSet<string> fileKeys = new(StringComparer.Ordinal);

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                ReadConfigText(path),
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new CommandLineException(
                    $"--config file must hold a JSON object of options: {path} "
                        + $"(got {KindName(document.RootElement.ValueKind)})");
            }

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

                // JSON admits a repeated key and the parser keeps both, so the file
                // takes the command line's rule; "dryRun" and "dry-run" are one key.
                if (!fileKeys.Add(Normalize(property.Name)))
                {
                    throw new CommandLineException(
                        $"Key '{property.Name}' is given more than once in {path}");
                }

                // null is "not set", like an absent key — never a switch turned on.
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                // Read for its kind first, so a blank "tables": [] is absent like "".
                string value = ConfigValue(property);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    values[Normalize(property.Name)] = value;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new CommandLineException(
                $"--config file is not valid JSON: {path} ({ex.Message})");
        }

        return values;
    }

    // An unreadable file is reported against the flag that named it; File.Exists
    // above has already turned a missing one (or a directory) into "not found".
    private static string ReadConfigText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CommandLineException($"--config file cannot be read: {path} ({ex.Message})");
        }
    }

    // Each option takes the kinds its command-line form can spell, so a value of
    // another kind is an error rather than a string: "output": false generated
    // into a directory named false.
    private static string ConfigValue(JsonProperty property)
    {
        string key = Normalize(property.Name);
        JsonValueKind kind = property.Value.ValueKind;

        if (Switches.Contains(key))
        {
            // Flag reports a number or a string other than true/false.
            return kind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.String or JsonValueKind.Number => property.Value.ToString(),
                _ => throw WrongKind(property, "true or false"),
            };
        }

        if (key == "tables")
        {
            return kind switch
            {
                JsonValueKind.String => property.Value.GetString()!,
                JsonValueKind.Array => JoinArray(property.Name, property.Value),
                _ => throw WrongKind(property, "a string or an array of strings"),
            };
        }

        return kind switch
        {
            JsonValueKind.String => property.Value.GetString()!,
            JsonValueKind.Number => property.Value.ToString(),
            _ => throw WrongKind(property, "a string or number"),
        };
    }

    private static CommandLineException WrongKind(JsonProperty property, string expected) =>
        new($"\"{property.Name}\" in the --config file must be {expected} "
            + $"(got {KindName(property.Value.ValueKind)})");

    private static string KindName(JsonValueKind kind) =>
        kind switch
        {
            JsonValueKind.Object => "an object",
            JsonValueKind.Array => "an array",
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            JsonValueKind.Null => "null",
            _ => "a boolean",
        };

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

        Dbms dbms = DbmsOption.Parse(Required(values, "dbms"));

        return new RunOptions(
            check ? RunMode.Check : fix ? RunMode.Fix : RunMode.Generate,
            BuildConnection(values, dbms),
            BuildSettings(values),
            dryRun: Flag(values, "dry-run"),
            json: format == "json",
            verbose: Flag(values, "verbose"));
    }

    private static DbConnectionInfo BuildConnection(
        Dictionary<string, string> values, Dbms dbms)
    {
        if (dbms == Dbms.Sqlite)
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
    private static int ResolvePort(Dictionary<string, string> values, Dbms dbms)
    {
        if (Value(values, "port") is not { } port)
        {
            return DbmsOption.DefaultPort(dbms);
        }

        return DbmsOption.TryParsePort(port, out int parsed)
            ? parsed
            : throw new CommandLineException(
                $"--port must be a number between 1 and 65535 (got '{port}')");
    }

    // MySQL has no schema layer above the database, and Oracle's schema is the user
    // unless one is named, so neither makes --schema mandatory.
    private static string ResolveSchema(
        Dictionary<string, string> values, Dbms dbms, string database, string user) =>
        dbms switch
        {
            Dbms.MySql => Value(values, "schema") ?? database,
            Dbms.Oracle => Value(values, "schema") ?? user,
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

    // The option surface is comma-separated, so an element carrying a comma
    // cannot round-trip — reject rather than silently split (release audit).
    private static string JoinArray(string name, JsonElement array)
    {
        if (array.EnumerateArray().Any(e => e.ValueKind == JsonValueKind.Null))
        {
            throw new CommandLineException($"\"{name}\" array elements must not be null");
        }

        if (array.EnumerateArray().Any(e => e.ValueKind != JsonValueKind.String))
        {
            throw new CommandLineException($"\"{name}\" array elements must be strings");
        }

        List<string> items = [.. array.EnumerateArray().Select(e => e.GetString()!)];

        return items.Any(item => item.Contains(','))
            ? throw new CommandLineException($"\"{name}\" array elements must not contain commas")
            : string.Join(",", items);
    }

    private static IReadOnlyList<string> SplitTables(string? tables) =>
        string.IsNullOrWhiteSpace(tables)
            ? []
            : [.. tables.Split(',', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)];

    // Never blank: ParseArguments and ReadConfigFile store no blank value.
    private static string? Value(Dictionary<string, string> values, string key) =>
        values.TryGetValue(Normalize(key), out string? value) ? value : null;

    private static string Required(Dictionary<string, string> values, string key) =>
        Value(values, key) is { } value
            ? value
            : throw new CommandLineException(
                $"--{key} is required (or set \"{key}\" in the --config file)");

    // A switch is true or false, nothing else: a config-file `"fix": 0` or `"no"`
    // read as on would run the mode that rewrites files.
    private static bool Flag(Dictionary<string, string> values, string key) =>
        Value(values, key) switch
        {
            null => false,
            { } value when value.Equals("true", StringComparison.OrdinalIgnoreCase) => true,
            { } value when value.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
            { } value => throw new CommandLineException(
                $"\"{key}\" in the --config file must be true or false (got '{value}')"),
        };
}
