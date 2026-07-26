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

        List<(string Name, string Type, bool NotNull, bool HasDefault, int Pk)> rows = [];
        using (IDbCommand command = conn.CreateCommand())
        {
            command.CommandText =
                "SELECT name, type, \"notnull\", dflt_value, pk FROM pragma_table_info(@table)";
            AddParameter(command, "@table", tableName);

            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((
                    NormalizeName(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetInt32(2) != 0,
                    !reader.IsDBNull(3),
                    reader.GetInt32(4)));
            }
        }

        if (rows.Count == 0)
        {
            return false;
        }

        int keyColumnCount = rows.Count(r => r.Pk > 0);
        bool hasPkIndex = HasPkOriginIndex(conn, tableName);

        List<DbColumnInfo> columns = [];
        foreach ((string Name, string Type, bool NotNull, bool HasDefault, int Pk) row in rows)
        {
            // A lone INTEGER PRIMARY KEY aliases the rowid: the pragma reports it as
            // nullable with no default, yet it never holds NULL and is auto-assigned.
            // PRIMARY KEY DESC and WITHOUT ROWID look identical in table_info but are
            // real keys, not aliases; both are told apart by their pk-origin index,
            // which a genuine alias never has.
            bool isRowIdAlias = keyColumnCount == 1
                && row.Pk == 1
                && string.Equals(row.Type, "INTEGER", StringComparison.OrdinalIgnoreCase)
                && !hasPkIndex;

            columns.Add(new DbColumnInfo(
                row.Name,
                row.Type,
                isNullable: !isRowIdAlias && !row.NotNull,
                hasDefault: isRowIdAlias || row.HasDefault));
        }

        table = new DbTableInfo(tableName, columns);
        return true;
    }

    private static bool HasPkOriginIndex(IDbConnection conn, string tableName)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM pragma_index_list(@table) WHERE origin = 'pk'";
        AddParameter(command, "@table", tableName);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
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
