namespace SqlArtisan.TableClassGen;

internal sealed class ConsoleUI
{
    public DbConnectionInfo ReadDatabaseConnectionInfo()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter database information.");

        Console.Write("Database type (1.Oracle/2.PostgreSQL/3.MySQL/4.SQLite/5.SQLServer): ");
        string dbTypeInput = Console.ReadLine() ?? string.Empty;
        DbmsType dbType = ParseDatabaseType(dbTypeInput);

        // SQLite is file-based, so it skips the host/port/credentials prompts.
        if (dbType == DbmsType.Sqlite)
        {
            return ReadSqliteConnectionInfo();
        }

        Console.Write("Host: ");
        string host = Console.ReadLine() ?? "localhost";

        Console.Write("Port: ");
        string portStr = Console.ReadLine() ?? string.Empty;
        int port = string.IsNullOrWhiteSpace(portStr)
            ? DefaultPort(dbType)
            : int.Parse(portStr);

        Console.Write("Service name (or database name): ");
        string serviceName = Console.ReadLine() ?? string.Empty;

        string? schema = null;
        if (dbType == DbmsType.PostgreSql)
        {
            Console.Write("Schema: ");
            schema = Console.ReadLine() ?? string.Empty;
        }
        else if (dbType == DbmsType.SqlServer)
        {
            Console.Write("Schema (default dbo): ");
            string schemaInput = Console.ReadLine() ?? string.Empty;
            schema = string.IsNullOrWhiteSpace(schemaInput) ? "dbo" : schemaInput;
        }
        else if (dbType == DbmsType.MySql)
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
            dbType,
            host,
            port,
            serviceName,
            schema ?? username,
            username,
            password);
    }

    private static DbConnectionInfo ReadSqliteConnectionInfo()
    {
        Console.Write("Database file path: ");
        string filePath = Console.ReadLine() ?? string.Empty;

        return new DbConnectionInfo(
            DbmsType.Sqlite,
            string.Empty,
            0,
            filePath,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static int DefaultPort(DbmsType dbType) =>
        dbType switch
        {
            DbmsType.Oracle => 1521,
            DbmsType.PostgreSql => 5432,
            DbmsType.MySql => 3306,
            DbmsType.SqlServer => 1433,
            _ => throw new ArgumentOutOfRangeException(nameof(dbType))
        };

    public CodeGenerationSettings ReadCodeGenerationSettings()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter code generation settings.");

        Console.Write("Namespace: ");
        string @namespace = Console.ReadLine() ?? "SqlArtisan.TableDefinitions";

        Console.Write("Convert object names to lowercase (y/n): ");
        string lowercaseNamesStr = Console.ReadLine() ?? "n";
        bool lowercaseNames = lowercaseNamesStr.Trim().ToLower().StartsWith("y");

        Console.Write("Output directory: ");
        string outputDirectory = Console.ReadLine() ?? ".";

        Console.Write("Create subfolders by table name initial (y/n): ");
        string createSubFoldersStr = Console.ReadLine() ?? "n";
        bool createSubFolders = createSubFoldersStr.Trim().ToLower().StartsWith("y");

        Console.Write("Specific table name (leave empty for all tables): ");
        string specificTableName = Console.ReadLine() ?? string.Empty;
        specificTableName = string.IsNullOrWhiteSpace(specificTableName)
            ? string.Empty
            : specificTableName;

        return new CodeGenerationSettings(
            @namespace,
            lowercaseNames,
            outputDirectory,
            createSubFolders,
            specificTableName);
    }

    private static DbmsType ParseDatabaseType(string dbTypeInput)
    {
        return dbTypeInput.Trim().ToLowerInvariant() switch
        {
            "1" or "oracle" => DbmsType.Oracle,
            "2" or "postgres" => DbmsType.PostgreSql,
            "3" or "mysql" => DbmsType.MySql,
            "4" or "sqlite" => DbmsType.Sqlite,
            "5" or "sqlserver" or "mssql" => DbmsType.SqlServer,
            _ => throw new ArgumentException($"Unsupported database type: {dbTypeInput}")
        };
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
