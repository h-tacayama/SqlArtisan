using System.Data;
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
            .Where(t.Owner == _connInfo.Schema.ToUpper())
            .OrderBy(t.TableName);

        List<CatalogTable> tables = [];

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

        AllTabColumns atc = new();

        // DEFAULT_LENGTH stands in for DATA_DEFAULT, which is a LONG needing
        // provider-specific retrieval; only its presence matters here.
        ISqlBuilder sql =
            Select(
                atc.ColumnName,
                atc.DataType,
                atc.Nullable,
                atc.DefaultLength,
                atc.IdentityColumn)
            .From(atc)
            .Where(
                atc.Owner == _connInfo.Schema.ToUpper()
                & atc.TableName == tableName.ToUpper())
            .OrderBy(atc.ColumnId);

        ColumnIndexInfo indexes =
            new CatalogColumnIndexReader(Dbms.Oracle, _connInfo.Schema)
                .Read(conn, tableName);

        List<CatalogColumn> columns = [];

        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                string catalogName = reader.GetString(0);
                string dataType = reader.GetString(1);
                columns.Add(new CatalogColumn(
                    _lowercaseNames ? catalogName.ToLower() : catalogName,
                    dataType,
                    isNullable: ReadIsNullable(reader, 2),
                    hasDefault: ReadHasDefault(reader, 3, 4),
                    isIndexed: indexes.IsIndexed(catalogName),
                    dbms: Dbms.Oracle));
            }
        }

        if (columns.Count == 0)
        {
            return false;
        }

        table = new CatalogTable(_lowercaseNames
            ? tableName.ToLower()
            : tableName.ToUpper(),
            columns,
            _connInfo.Schema.ToUpper());

        return true;
    }

    // Decidable here, unlike on information_schema: Oracle records an identity
    // column's sequence and a virtual column's expression in DATA_DEFAULT, and flags
    // identity separately — so an absent default really is no default.
    private static bool ReadHasDefault(IDataReader reader, int lengthOrdinal, int identityOrdinal) =>
        !reader.IsDBNull(lengthOrdinal)
        || (!reader.IsDBNull(identityOrdinal)
            && string.Equals(reader.GetString(identityOrdinal), "YES", StringComparison.OrdinalIgnoreCase));

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
