using System.Data;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.TableClassGen;

internal sealed class OracleCatalogReader(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ICatalogReader
{
    // ORA-00904, which a catalog without DATA_DEFAULT_VC raises for it.
    private const int InvalidIdentifier = 904;

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

        List<CatalogColumn> columns;

        try
        {
            columns = ReadColumns(conn, tableName, indexes, withDefaultText: true);
        }
        catch (OracleException ex) when (ex.Number == InvalidIdentifier)
        {
            // Without DATA_DEFAULT_VC a DEFAULT NULL reads as a default, as it did
            // everywhere before #645.
            columns = ReadColumns(conn, tableName, indexes, withDefaultText: false);
        }

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
        bool withDefaultText)
    {
        AllTabColumns atc = new();

        // DEFAULT_LENGTH, not DATA_DEFAULT: see AllTabColumns.DefaultLength.
        ISqlBuilder sql =
            Select(
                atc.ColumnName,
                atc.DataType,
                atc.Nullable,
                atc.DefaultLength,
                atc.IdentityColumn,
                withDefaultText ? atc.DataDefaultVc : Null)
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
                hasDefault: ReadHasDefault(reader),
                isIndexed: indexes.IsIndexed(catalogName),
                dbms: Dbms.Oracle));
        }

        return columns;
    }

    // Decidable here, unlike on information_schema: Oracle records identity and virtual
    // columns in DATA_DEFAULT and flags identity apart. A DEFAULT NULL text is no
    // default; one past 4000 characters reads null in DATA_DEFAULT_VC, so it counts.
    private static bool ReadHasDefault(IDataReader reader) =>
        (!reader.IsDBNull(3)
            && (reader.IsDBNull(5) || !DefaultExpression.IsNull(reader.GetString(5))))
        || (!reader.IsDBNull(4)
            && string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase));

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
