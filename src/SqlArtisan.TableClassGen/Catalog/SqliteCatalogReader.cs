using System.Data;

namespace SqlArtisan.TableClassGen;

// sqlite_master and pragma_table_info are catalog surfaces the SqlArtisan
// builder cannot express, so this reader uses raw ADO.NET. SQLite has no
// schema concept, so _connInfo.Schema is unused.
internal sealed class SqliteCatalogReader(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ICatalogReader
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<CatalogTable> GetAllTables()
    {
        using IDbConnection conn = _connInfo.OpenConnection();

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

        List<CatalogTable> tables = [];
        foreach (string tableName in tableNames)
        {
            if (TryGetTable(conn, tableName, out CatalogTable? table)
                && table is not null)
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    public bool TryGetTable(string tableName, out CatalogTable? table)
    {
        using IDbConnection conn = _connInfo.OpenConnection();

        return TryGetTable(conn, tableName, out table);
    }

    private bool TryGetTable(IDbConnection conn, string tableName, out CatalogTable? table)
    {
        table = null;

        List<(string Name, string CatalogName, string Type, bool NotNull, bool HasDefault, int Pk)> rows = [];
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
                    reader.GetString(0),
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
        ColumnIndexInfo indexes = new SqliteColumnIndexReader().Read(conn, tableName);

        List<CatalogColumn> columns = [];
        foreach ((string Name, string CatalogName, string Type, bool NotNull, bool HasDefault, int Pk) row in rows)
        {
            // table_info reports rowid aliases and real keys identically; the
            // discriminator is the pk-origin index a genuine alias never has,
            // not the DESC or WITHOUT ROWID wording.
            bool isRowIdAlias = keyColumnCount == 1
                && row.Pk == 1
                && string.Equals(row.Type, "INTEGER", StringComparison.OrdinalIgnoreCase)
                && !hasPkIndex;

            // The alias has no index row of its own, yet a predicate on it is a rowid
            // lookup (EXPLAIN QUERY PLAN), which wrapping loses like any indexed column.
            columns.Add(new CatalogColumn(
                row.Name,
                row.Type,
                isNullable: !isRowIdAlias && !row.NotNull,
                hasDefault: isRowIdAlias || row.HasDefault,
                isIndexed: isRowIdAlias ? true : indexes.IsIndexed(row.CatalogName),
                dbms: Dbms.Sqlite));
        }

        // Normalized here, at the construction site, like the sibling readers — the
        // public TryGetTable path receives the user's spelling, not a normalized one.
        table = new CatalogTable(NormalizeName(tableName), columns);
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
        _lowercaseNames ? name.ToLowerInvariant() : name;

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
