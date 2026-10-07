using System.Data;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.TableClassGen;

internal sealed class OracleCatalogReader(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ICatalogReader
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<CatalogTable> GetAllTables()
    {
        using IDbConnection conn = _connInfo.OpenConnection();

        AllTables t = new();

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(t.Owner == _connInfo.Schema.ToUpperInvariant())
            .OrderBy(t.TableName);

        List<CatalogTable> tables = [];

        List<string> tableNames = [];
        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                string tableName = _lowercaseNames
                    ? reader.GetString(0).ToLowerInvariant()
                    : reader.GetString(0);
                tableNames.Add(tableName);
            }
        }

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

        if (!ExistsTable(conn, tableName))
        {
            return false;
        }

        ColumnIndexInfo indexes =
            new CatalogColumnIndexReader(Dbms.Oracle, _connInfo.Schema)
                .Read(conn, tableName);

        List<CatalogColumn> columns =
            ReadColumns(conn, tableName, indexes, ReadNullDefaults(conn, tableName));

        // Oracle has no column-less table, so an empty list is a privilege gap.
        if (columns.Count == 0)
        {
            throw new CommandLineException(_connInfo.NoVisibleColumnsMessage(tableName));
        }

        table = new CatalogTable(_lowercaseNames
            ? tableName.ToLowerInvariant()
            : tableName.ToUpperInvariant(),
            columns,
            _connInfo.Schema.ToUpperInvariant());

        return true;
    }

    private List<CatalogColumn> ReadColumns(
        IDbConnection conn,
        string tableName,
        ColumnIndexInfo indexes,
        IReadOnlySet<string> nullDefaults)
    {
        AllTabColumns atc = new();

        // DEFAULT_LENGTH, not DATA_DEFAULT: see AllTabColumns.DefaultLength.
        ISqlBuilder sql =
            Select(
                atc.ColumnName,
                atc.DataType,
                atc.Nullable,
                atc.DefaultLength,
                atc.IdentityColumn)
            .From(atc)
            .Where(
                atc.Owner == _connInfo.Schema.ToUpperInvariant()
                & atc.TableName == tableName.ToUpperInvariant())
            .OrderBy(atc.ColumnId);

        List<CatalogColumn> columns = [];

        using IDataReader reader = conn.ExecuteReader(sql);
        while (reader.Read())
        {
            string catalogName = reader.GetString(0);
            string dataType = reader.GetString(1);
            columns.Add(new CatalogColumn(
                _lowercaseNames ? catalogName.ToLowerInvariant() : catalogName,
                dataType,
                isNullable: ReadIsNullable(reader, 2),
                hasDefault: ReadHasDefault(reader, nullDefaults.Contains(catalogName)),
                isIndexed: indexes.IsIndexed(catalogName),
                dbms: Dbms.Oracle));
        }

        return columns;
    }

    // Decidable here, unlike on information_schema: Oracle records identity and virtual
    // columns in DATA_DEFAULT and flags identity apart, so an absent default really is
    // none. A DEFAULT NULL keeps DEFAULT_LENGTH set, yet supplies nothing.
    private static bool ReadHasDefault(IDataReader reader, bool nullDefault) =>
        (!reader.IsDBNull(3) && !nullDefault)
        || (!reader.IsDBNull(4)
            && string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase));

    // DATA_DEFAULT is a LONG, which ODP.NET reads only with a fetch size set, so it
    // is read apart, and only for the columns that have a default at all.
    private HashSet<string> ReadNullDefaults(IDbConnection conn, string tableName)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText =
            """
            SELECT COLUMN_NAME, DATA_DEFAULT
            FROM ALL_TAB_COLUMNS
            WHERE OWNER = :schema_name AND TABLE_NAME = :table_name
                AND DEFAULT_LENGTH IS NOT NULL
            """;
        CatalogCommand.AddParameter(command, ":schema_name", _connInfo.Schema.ToUpperInvariant());
        CatalogCommand.AddParameter(command, ":table_name", tableName.ToUpperInvariant());

        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.InitialLONGFetchSize = -1;
        }

        HashSet<string> nullDefaults = new(StringComparer.Ordinal);

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(1) && DefaultExpression.IsNull(reader.GetString(1)))
            {
                nullDefaults.Add(reader.GetString(0));
            }
        }

        return nullDefaults;
    }

    private static bool? ReadIsNullable(IDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : string.Equals(reader.GetString(ordinal), "Y", StringComparison.OrdinalIgnoreCase);

    private bool ExistsTable(IDbConnection conn, string tableName)
    {
        AllTables t = new();

        ISqlBuilder sql =
            Select(Count(t.TableName))
            .From(t)
            .Where(
                t.Owner == _connInfo.Schema.ToUpperInvariant()
                & t.TableName == tableName.ToUpperInvariant());

        int tableCount = Convert.ToInt32(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
