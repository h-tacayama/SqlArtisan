using System.Data;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.TableClassGen;

internal sealed class OracleTableInfoRepository(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ITableInfoRepository
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<DbTableInfo> GetAllTables()
    {
        using IDbConnection conn = _connInfo.CreateConnection();
        conn.Open();

        AllTables t = new();

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(t.Owner == _connInfo.Schema.ToUpper())
            .OrderBy(t.TableName);

        List<DbTableInfo> tables = [];

        List<string> tableNames = [];
        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                string tableName = _lowercaseNames
                    ? reader.GetString(0).ToLower()
                    : reader.GetString(0);
                tableNames.Add(tableName);
            }
        }

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

        if (!ExistsTable(conn, tableName))
        {
            return false;
        }

        AllTabColumns atc = new();

        // DATA_DEFAULT is deliberately not read: it is a LONG, whose retrieval needs
        // provider-specific handling, and identity columns carry their own flag — so
        // HasDefault stays unknown here until both are verified against a live engine.
        ISqlBuilder sql =
            Select(
                atc.ColumnName,
                atc.DataType,
                atc.Nullable)
            .From(atc)
            .Where(
                atc.Owner == _connInfo.Schema.ToUpper()
                & atc.TableName == tableName.ToUpper())
            .OrderBy(atc.ColumnId);

        ColumnIndexInfo indexes =
            new CatalogColumnIndexRepository(DbmsType.Oracle, _connInfo.Schema)
                .Read(conn, tableName);

        List<DbColumnInfo> columns = [];

        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                string catalogName = reader.GetString(0);
                string dataType = reader.GetString(1);
                columns.Add(new DbColumnInfo(
                    _lowercaseNames ? catalogName.ToLower() : catalogName,
                    dataType,
                    isNullable: ReadIsNullable(reader, 2),
                    isIndexed: indexes.IsIndexed(catalogName)));
            }
        }

        if (columns.Count == 0)
        {
            return false;
        }

        table = new DbTableInfo(_lowercaseNames
            ? tableName.ToLower()
            : tableName.ToUpper(),
            columns,
            _connInfo.Schema.ToUpper());

        return true;
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
                t.Owner == _connInfo.Schema.ToUpper()
                & t.TableName == tableName.ToUpper());

        int tableCount = Convert.ToInt32(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
