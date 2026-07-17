using System.Data;

namespace SqlArtisan.TableClassGen;

// SQLite exposes its catalog through sqlite_master and the pragma_table_info
// table-valued function, neither of which the SqlArtisan builder can express, so
// this repository reads them with raw ADO.NET. SQLite has no schema concept, so
// _connInfo.Schema is unused.
internal sealed class SqliteTableInfoRepository(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ITableInfoRepository
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<DbTableInfo> GetAllTables()
    {
        using IDbConnection conn = _connInfo.CreateConnection();
        conn.Open();

        List<string> tableNames = [];
        using (IDbCommand command = conn.CreateCommand())
        {
            command.CommandText =
                "SELECT name FROM sqlite_master "
                + "WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' "
                + "ORDER BY name";

            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableNames.Add(NormalizeName(reader.GetString(0)));
            }
        }

        List<DbTableInfo> tables = [];
        foreach (string tableName in tableNames)
        {
            if (TryGetTableInfo(conn, tableName, out DbTableInfo? table)
                && table is not null)
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    public bool TryGetTableInfo(string tableName, out DbTableInfo? table)
    {
        using IDbConnection conn = _connInfo.CreateConnection();
        conn.Open();

        return TryGetTableInfo(conn, tableName, out table);
    }

    private bool TryGetTableInfo(IDbConnection conn, string tableName, out DbTableInfo? table)
    {
        table = null;

        List<DbColumnInfo> columns = [];
        using (IDbCommand command = conn.CreateCommand())
        {
            command.CommandText = "SELECT name, type FROM pragma_table_info(@table)";
            AddParameter(command, "@table", tableName);

            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string columnName = NormalizeName(reader.GetString(0));
                string dataType = reader.GetString(1);
                columns.Add(new DbColumnInfo(columnName, dataType));
            }
        }

        if (columns.Count == 0)
        {
            return false;
        }

        table = new DbTableInfo(tableName, columns);
        return true;
    }

    private string NormalizeName(string name) =>
        _lowercaseNames ? name.ToLower() : name;

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
