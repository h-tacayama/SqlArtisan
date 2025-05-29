using System.Collections.Concurrent;
using System.Data;

namespace SqlArtisan;

public static class DbmsResolver
{
    private static readonly ConcurrentDictionary<string, Dbms> s_providerMap =
        new(StringComparer.OrdinalIgnoreCase);

    static DbmsResolver()
    {
        RegisterProvider("Microsoft.Data.SqlClient.SqlConnection", Dbms.SqlServer);
        RegisterProvider("System.Data.SqlClient.SqlConnection", Dbms.SqlServer);

        RegisterProvider("Npgsql.NpgsqlConnection", Dbms.PostgreSql);

        RegisterProvider("MySql.Data.MySqlClient.MySqlConnection", Dbms.MySql);
        RegisterProvider("MySqlConnector.MySqlConnection", Dbms.MySql);

        RegisterProvider("Microsoft.Data.Sqlite.SqliteConnection", Dbms.Sqlite);
        RegisterProvider("System.Data.SQLite.SQLiteConnection", Dbms.Sqlite);

        RegisterProvider("Oracle.ManagedDataAccess.Client.OracleConnection", Dbms.Oracle);
        RegisterProvider("Oracle.DataAccess.Client.OracleConnection", Dbms.Oracle);
    }

    public static void RegisterProvider(string typeFullName, Dbms dbms)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentNullException(nameof(typeFullName));
        }

        s_providerMap.TryAdd(typeFullName, dbms);
    }

    public static Dbms Resolve(IDbConnection connection)
    {
        if (connection == null)
        {
            return Dbms.Unknown;
        }

        string typeFullName = connection.GetType()?.FullName ?? string.Empty;
        s_providerMap.TryGetValue(typeFullName, out Dbms identified);

        return identified;
    }


}
