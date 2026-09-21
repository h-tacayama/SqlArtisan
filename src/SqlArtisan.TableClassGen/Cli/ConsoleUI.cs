namespace SqlArtisan.TableClassGen;

internal sealed class ConsoleUI
{
    public DbConnectionInfo ReadDatabaseConnectionInfo()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter database information.");

        Dbms dbms = ReadDatabaseType();

        // SQLite is file-based, so it skips the host/port/credentials prompts.
        if (dbms == Dbms.Sqlite)
        {
            return ReadSqliteConnectionInfo();
        }

        // ReadLine returns "" on a blank Enter (null only at EOF), so every default
        // here goes through IsNullOrWhiteSpace — the idiom ReadPort set (#430).
        Console.Write("Host (default localhost): ");
        string hostInput = Console.ReadLine() ?? string.Empty;
        string host = string.IsNullOrWhiteSpace(hostInput) ? "localhost" : hostInput;

        int port = ReadPort(dbms);

        string serviceName = ReadRequired("Service name (or database name): ", "database name");

        string? schema = null;
        if (dbms == Dbms.PostgreSql)
        {
            Console.Write("Schema (default public): ");
            string schemaInput = Console.ReadLine() ?? string.Empty;
            schema = string.IsNullOrWhiteSpace(schemaInput) ? "public" : schemaInput;
        }
        else if (dbms == Dbms.SqlServer)
        {
            Console.Write("Schema (default dbo): ");
            string schemaInput = Console.ReadLine() ?? string.Empty;
            schema = string.IsNullOrWhiteSpace(schemaInput) ? "dbo" : schemaInput;
        }
        else if (dbms == Dbms.MySql)
        {
            // MySQL has no schema layer above the database, so information_schema
            // is filtered by the database name itself.
            schema = serviceName;
        }

        string username = ReadRequired("Username: ", "user name");

        Console.Write("Password: ");
        string password = GetPasswordFromConsole();

        return new DbConnectionInfo(
            dbms,
            host,
            port,
            serviceName,
            schema ?? username,
            username,
            password);
    }

    // Re-prompted like ReadPort: a blank answer here would otherwise surface as a
    // driver error far from the prompt, where the CLI path says "--x is required".
    private static string ReadRequired(string prompt, string what)
    {
        while (true)
        {
            Console.Write(prompt);
            string answer = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(answer))
            {
                return answer;
            }

            Console.WriteLine($"A {what} is required.");
        }
    }

    private static Dbms ReadDatabaseType()
    {
        while (true)
        {
            Console.Write(DatabaseTypePrompt);
            string answer = Console.ReadLine() ?? string.Empty;
            try
            {
                return ParseDatabaseType(answer);
            }
            catch (CommandLineException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    internal static int ReadPort(Dbms dbms)
    {
        while (true)
        {
            Console.Write("Port: ");
            string portStr = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(portStr))
            {
                return DbmsOption.DefaultPort(dbms);
            }

            if (DbmsOption.TryParsePort(portStr, out int port))
            {
                return port;
            }

            Console.WriteLine("Enter a port number between 1 and 65535.");
        }
    }

    private static DbConnectionInfo ReadSqliteConnectionInfo()
    {
        string filePath = ReadRequired("Database file path: ", "file path");

        return new DbConnectionInfo(
            Dbms.Sqlite,
            string.Empty,
            0,
            filePath,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    public CodeGenerationSettings ReadCodeGenerationSettings()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter code generation settings.");

        Console.Write("Namespace (default SqlArtisan.TableDefinitions): ");
        string namespaceInput = Console.ReadLine() ?? string.Empty;
        string @namespace = string.IsNullOrWhiteSpace(namespaceInput)
            ? "SqlArtisan.TableDefinitions"
            : namespaceInput;

        Console.Write("Convert object names to lowercase (y/n): ");
        string lowercaseNamesStr = Console.ReadLine() ?? "n";
        bool lowercaseNames =
            lowercaseNamesStr.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);

        Console.Write("Output directory (default .): ");
        string outputInput = Console.ReadLine() ?? string.Empty;
        string outputDirectory = string.IsNullOrWhiteSpace(outputInput) ? "." : outputInput;

        Console.Write("Create subfolders by table name initial (y/n): ");
        string createSubFoldersStr = Console.ReadLine() ?? "n";
        bool createSubFolders =
            createSubFoldersStr.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);

        Console.Write("Specific table name (leave empty for all tables): ");
        string specificTableName = Console.ReadLine() ?? string.Empty;

        return new CodeGenerationSettings(
            @namespace,
            lowercaseNames,
            outputDirectory,
            createSubFolders,
            string.IsNullOrWhiteSpace(specificTableName) ? [] : [specificTableName.Trim()]);
    }

    // The prompt is rendered from this table and the numbers are read back from
    // it, so a choice can never be offered by a number the parser does not know.
    private static readonly (string Label, Dbms Dbms)[] Choices =
    [
        ("Oracle", Dbms.Oracle),
        ("PostgreSQL", Dbms.PostgreSql),
        ("MySQL", Dbms.MySql),
        ("SQLite", Dbms.Sqlite),
        ("SQLServer", Dbms.SqlServer),
    ];

    internal static string DatabaseTypePrompt =>
        $"Database type ({string.Join("/", Choices.Select((c, i) => $"{i + 1}.{c.Label}"))}): ";

    internal static Dbms ParseDatabaseType(string answer)
    {
        string value = answer.Trim();

        if (int.TryParse(value, out int choice) && choice >= 1 && choice <= Choices.Length)
        {
            return Choices[choice - 1].Dbms;
        }

        return DbmsOption.TryParse(value, out Dbms dbms)
            ? dbms
            : throw new CommandLineException(
                "Enter a number from the list, or one of mysql, oracle, postgresql (or "
                    + $"postgres), sqlite, sqlserver (or mssql) (got '{value}').");
    }

    private static string GetPasswordFromConsole()
    {
        string password = string.Empty;
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }
}
