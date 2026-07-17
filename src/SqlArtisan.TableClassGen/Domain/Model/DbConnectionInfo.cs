using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.TableClassGen;

internal sealed class DbConnectionInfo(
    DbmsType databaseType,
    string host,
    int port,
    string serviceName,
    string schema,
    string username,
    string password)
{
    public DbmsType DbmsType => databaseType;

    public string Host => host;

    public int Port => port;

    public string ServiceName => serviceName;

    public string Schema => schema;

    public string Username => username;

    public string Password => password;

    public IDbConnection CreateConnection() =>
        DbmsType switch
        {
            DbmsType.Oracle => new OracleConnection(GetConnectionString()),
            DbmsType.PostgreSql => new NpgsqlConnection(GetConnectionString()),
            DbmsType.MySql => new MySqlConnection(GetConnectionString()),
            DbmsType.Sqlite => new SqliteConnection(GetConnectionString()),
            DbmsType.SqlServer => new SqlConnection(GetConnectionString()),
            _ => throw new ArgumentOutOfRangeException(nameof(DbmsType))
        };

    private string GetConnectionString() =>
        DbmsType switch
        {
            DbmsType.Oracle =>
                $"User Id={Username};Password={Password};Data Source={Host}:{Port}/{ServiceName}",
            DbmsType.PostgreSql =>
                $"Host={Host};Port={Port};Database={ServiceName};Username={Username};Password={Password}",
            DbmsType.MySql =>
                $"Server={Host};Port={Port};Database={ServiceName};User ID={Username};Password={Password}",
            // SQLite is file-based: ServiceName carries the database path.
            DbmsType.Sqlite =>
                $"Data Source={ServiceName}",
            // SQL Server takes host,port (comma); TrustServerCertificate eases dev/container TLS.
            DbmsType.SqlServer =>
                $"Server={Host},{Port};Database={ServiceName};User ID={Username};Password={Password};TrustServerCertificate=True",
            _ => throw new ArgumentOutOfRangeException(nameof(DbmsType))
        };
}
