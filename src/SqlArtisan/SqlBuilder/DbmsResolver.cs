using System.Collections.Concurrent;
using System.Data;

namespace SqlArtisan;

/// <summary>
/// Maps an ADO.NET connection type to its <see cref="Dbms"/>, so a statement can be
/// built for the dialect of an open connection.
/// </summary>
/// <remarks>
/// The common providers (SQL Server, PostgreSQL, MySQL, SQLite, Oracle) are registered
/// at startup; register additional ones with <see cref="RegisterProvider(string, Dbms)"/>.
/// </remarks>
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

    /// <summary>
    /// Registers a connection type so <see cref="Resolve(IDbConnection)"/> maps it to <paramref name="dbms"/>. The first registration for a type wins; a later one for the same type is ignored.
    /// </summary>
    /// <param name="typeFullName">The connection's fully qualified type name (e.g. <c>Npgsql.NpgsqlConnection</c>), matched case-insensitively.</param>
    /// <param name="dbms">The engine the connection type talks to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="typeFullName"/> is null, empty, or whitespace.</exception>
    public static void RegisterProvider(string typeFullName, Dbms dbms)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentNullException(nameof(typeFullName));
        }

        s_providerMap.TryAdd(typeFullName, dbms);
    }

    /// <summary>
    /// Resolves the engine behind a connection from its runtime type.
    /// </summary>
    /// <param name="connection">The connection to inspect.</param>
    /// <returns>The registered <see cref="Dbms"/>, or <see cref="Dbms.Unknown"/> when <paramref name="connection"/> is null or its type is not registered.</returns>
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
