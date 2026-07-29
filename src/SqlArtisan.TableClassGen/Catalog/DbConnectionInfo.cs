using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.TableClassGen;

internal sealed class DbConnectionInfo(
    Dbms dbms,
    string host,
    int port,
    string serviceName,
    string schema,
    string username,
    string password)
{
    public Dbms Dbms => dbms;

    public string Host => host;

    public int Port => port;

    public string ServiceName => serviceName;

    public string Schema => schema;

    public string Username => username;

    public string Password => password;

    public IDbConnection OpenConnection()
    {
        IDbConnection connection = CreateConnection();

        try
        {
            connection.Open();
        }
        catch (Exception ex)
        {
            connection.Dispose();

            throw new CommandLineException(
                $"{CannotConnectMessage} The driver reported: {ex.Message}");
        }

        return connection;
    }

    // MySQL and Oracle default --schema to --database and --user, so naming --schema
    // alone would point a user at an option they never passed.
    public string EmptyCatalogMessage =>
        Dbms switch
        {
            Dbms.Sqlite =>
                $"No tables found in the SQLite database file '{ServiceName}'; check --file, "
                    + "since a path that does not exist is created empty rather than rejected",
            Dbms.MySql =>
                $"No tables found in schema '{Schema}'; check --schema, or --database, which "
                    + "it defaults to (see --help)",
            Dbms.Oracle =>
                $"No tables found in schema '{Schema}'; check --schema, or --user, which it "
                    + "defaults to (see --help)",
            _ =>
                $"No tables found in schema '{Schema}'; check --schema and --database "
                    + "(see --help)",
        };

    // The driver reports what it observed, never which option produced it, so the
    // options that built the connection string are named here.
    private string CannotConnectMessage =>
        Dbms switch
        {
            Dbms.Sqlite => $"Cannot open the SQLite database file '{ServiceName}' (--file).",
            _ =>
                $"Cannot connect to {Host}:{Port} as '{Username}'; check --host, --port, "
                    + "--database, --user, and SQLARTISAN_DB_PASSWORD (see --help).",
        };

    private IDbConnection CreateConnection() =>
        Dbms switch
        {
            Dbms.Oracle => new OracleConnection(GetConnectionString()),
            Dbms.PostgreSql => new NpgsqlConnection(GetConnectionString()),
            Dbms.MySql => new MySqlConnection(GetConnectionString()),
            Dbms.Sqlite => new SqliteConnection(GetConnectionString()),
            Dbms.SqlServer => new SqlConnection(GetConnectionString()),
            _ => throw new ArgumentOutOfRangeException(nameof(Dbms))
        };

    private string GetConnectionString() =>
        Dbms switch
        {
            Dbms.Oracle =>
                $"User Id={Username};Password={Password};Data Source={Host}:{Port}/{ServiceName}",
            Dbms.PostgreSql =>
                $"Host={Host};Port={Port};Database={ServiceName};Username={Username};Password={Password}",
            Dbms.MySql =>
                $"Server={Host};Port={Port};Database={ServiceName};User ID={Username};Password={Password}",
            // SQLite is file-based: ServiceName carries the database path.
            Dbms.Sqlite =>
                $"Data Source={ServiceName}",
            // SQL Server takes host,port (comma); TrustServerCertificate eases dev/container TLS.
            Dbms.SqlServer =>
                $"Server={Host},{Port};Database={ServiceName};User ID={Username};Password={Password};TrustServerCertificate=True",
            _ => throw new ArgumentOutOfRangeException(nameof(Dbms))
        };
}
