using System.Data;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.TableClassGen;

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

        ALL_TABLES t = new("t");

        ISqlBuilder sql =
            SELECT(t.TABLE_NAME)
            .FROM(t)
            .WHERE(t.OWNER == _connInfo.Schema.ToUpper())
            .ORDER_BY(t.TABLE_NAME);

        List<DbTableInfo> tables = new();

        List<string> tableNames = new();
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

        ALL_TAB_COLUMNS atc = new("atc");

        ISqlBuilder sql =
            SELECT(
                atc.COLUMN_NAME,
                atc.DATA_TYPE)
            .FROM(atc)
            .WHERE(
                AND(
                    atc.OWNER == _connInfo.Schema.ToUpper(),
                    atc.TABLE_NAME == tableName.ToUpper()));

        List<DbColumnInfo> columns = new();

        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                string columnName = _lowercaseNames
                    ? reader.GetString(0).ToLower()
                    : reader.GetString(0);
                string dataType = reader.GetString(1);
                columns.Add(new DbColumnInfo(columnName, dataType));
            }
        }

        if (columns.Count == 0)
        {
            return false;
        }

        table = new DbTableInfo(_lowercaseNames
            ? tableName.ToLower()
            : tableName.ToUpper(),
            columns);

        return true;
    }

    private bool ExistsTable(IDbConnection conn, string tableName)
    {
        ALL_TABLES t = new("t");

        ISqlBuilder sql =
            SELECT(COUNT(t.TABLE_NAME))
            .FROM(t)
            .WHERE(
                AND(
                    t.OWNER == _connInfo.Schema.ToUpper(),
                    t.TABLE_NAME == tableName.ToUpper()));

        int tableCount = Convert.ToInt32(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
