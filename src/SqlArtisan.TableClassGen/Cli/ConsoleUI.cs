namespace SqlArtisan.TableClassGen;

internal sealed class ConsoleUI
{
    public DbConnectionInfo ReadDatabaseConnectionInfo()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter database information.");

        Console.Write(DatabaseTypePrompt);
        string answer = Console.ReadLine() ?? string.Empty;
        Dbms dbms = ParseDatabaseType(answer);

        // SQLite is file-based, so it skips the host/port/credentials prompts.
        if (dbms == Dbms.Sqlite)
        {
            return ReadSqliteConnectionInfo();
        }

        Console.Write("Host: ");
        string host = Console.ReadLine() ?? "localhost";

        int port = ReadPort(dbms);

        Console.Write("Service name (or database name): ");
        string serviceName = Console.ReadLine() ?? string.Empty;

        string? schema = null;
        if (dbms == Dbms.PostgreSql)
        {
            Console.Write("Schema: ");
            schema = Console.ReadLine() ?? string.Empty;
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

        Console.Write("Username: ");
        string username = Console.ReadLine() ?? string.Empty;

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

            if (int.TryParse(portStr, out int port) && port is > 0 and <= 65535)
            {
                return port;
            }

            Console.WriteLine("Enter a port number between 1 and 65535.");
        }
    }

    private static DbConnectionInfo ReadSqliteConnectionInfo()
    {
        Console.Write("Database file path: ");
        string filePath = Console.ReadLine() ?? string.Empty;

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

        Console.Write("Namespace: ");
        string @namespace = Console.ReadLine() ?? "SqlArtisan.TableDefinitions";

        Console.Write("Convert object names to lowercase (y/n): ");
        string lowercaseNamesStr = Console.ReadLine() ?? "n";
        bool lowercaseNames =
            lowercaseNamesStr.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);

        Console.Write("Output directory: ");
        string outputDirectory = Console.ReadLine() ?? ".";

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

        return int.TryParse(value, out int choice) && choice >= 1 && choice <= Choices.Length
            ? Choices[choice - 1].Dbms
            : DbmsOption.Parse(value);
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

    public void ShowProgress(string message)
    {
        Console.WriteLine(message);
    }

    public void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    public void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
