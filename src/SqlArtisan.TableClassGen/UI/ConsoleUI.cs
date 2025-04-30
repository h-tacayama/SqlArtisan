namespace SqlArtisan.TableClassGen;

internal sealed class ConsoleUI
{
    public DbConnectionInfo ReadDatabaseConnectionInfo()
    {
        Console.WriteLine();
        Console.WriteLine("Please enter database information.");

        Console.Write("Database type (1.Oracle/2.PostgreSQL): ");
        string dbTypeInput = Console.ReadLine() ?? string.Empty;
        DbmsType dbType = ParseDatabaseType(dbTypeInput);

        Console.Write("Host: ");
        string host = Console.ReadLine() ?? "localhost";

        Console.Write("Port: ");
        string portStr = Console.ReadLine() ?? string.Empty;
        int port = string.IsNullOrWhiteSpace(portStr) ?
            (dbType == DbmsType.Oracle ? 1521 : 5432) :
            int.Parse(portStr);

        Console.Write("Service name (or database name): ");
        string serviceName = Console.ReadLine() ?? string.Empty;

        string? schema = null;
        if (dbType == DbmsType.PostgreSQL)
        {
            Console.Write("Schema: ");
            schema = Console.ReadLine() ?? string.Empty;
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
            "1" => DbmsType.Oracle,
            "2" or "postgres" => DbmsType.PostgreSQL,
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
