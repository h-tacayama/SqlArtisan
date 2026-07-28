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

    public IDbConnection CreateConnection() =>
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
