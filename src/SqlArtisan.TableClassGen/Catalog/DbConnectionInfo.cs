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
        // CreateConnection sits inside the guard too: a malformed connection string
        // throws at construction on most drivers, before Open is ever reached.
        IDbConnection? connection = null;

        try
        {
            connection = CreateConnection();
            connection.Open();
            ReadHeader(connection);
        }
        catch (Exception ex)
        {
            connection?.Dispose();

            throw new CommandLineException(
                $"{CannotConnectMessage} The driver reported: {ex.Message}");
        }

        return connection;
    }

    // Microsoft.Data.Sqlite opens lazily: a file that is not a database opens
    // fine and fails at the first query, which would reach stderr naming no --file.
    private void ReadHeader(IDbConnection connection)
    {
        if (Dbms != Dbms.Sqlite)
        {
            return;
        }

        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA schema_version";
        command.ExecuteScalar();
    }

    // MySQL and Oracle default --schema to --database and --user, so naming --schema
    // alone would point a user at an option they never passed.
    public string EmptyCatalogMessage =>
        Dbms switch
        {
            Dbms.Sqlite =>
                $"No tables found in the SQLite database file '{ServiceName}'; check --file",
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

    // A run that skipped the table would leave its committed class to read as removed,
    // and one that wrote only the visible columns would strip the rest from that class.
    public string NoVisibleColumnsMessage(string tableName) =>
        $"Not every column of table '{tableName}' is visible to --user '{Username}'; "
            + "grant SELECT on its columns, or name only the other tables with --tables";

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

    // Each driver's own builder, never string interpolation: a raw value
    // carrying `;` (a password of `x;Database=evil`, say) would otherwise
    // inject its own key/value pairs and silently redirect the connection.
    internal string GetConnectionString() =>
        Dbms switch
        {
            Dbms.Oracle => new OracleConnectionStringBuilder
            {
                UserID = Username,
                Password = Password,
                DataSource = $"{Host}:{Port}/{ServiceName}",
            }.ConnectionString,
            Dbms.PostgreSql => new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = ServiceName,
                Username = Username,
                Password = Password,
            }.ConnectionString,
            Dbms.MySql => new MySqlConnectionStringBuilder
            {
                Server = Host,
                Port = (uint)Port,
                Database = ServiceName,
                UserID = Username,
                Password = Password,
            }.ConnectionString,
            // SQLite is file-based: ServiceName carries the database path. The default
            // ReadWriteCreate would open a mistyped --file as an empty catalog.
            Dbms.Sqlite => new SqliteConnectionStringBuilder
            {
                DataSource = ServiceName,
                Mode = SqliteOpenMode.ReadWrite,
            }.ConnectionString,
            // SQL Server takes host,port (comma); TrustServerCertificate eases dev/container TLS.
            Dbms.SqlServer => new SqlConnectionStringBuilder
            {
                DataSource = $"{Host},{Port}",
                InitialCatalog = ServiceName,
                UserID = Username,
                Password = Password,
                TrustServerCertificate = true,
            }.ConnectionString,
            _ => throw new ArgumentOutOfRangeException(nameof(Dbms))
        };
}
