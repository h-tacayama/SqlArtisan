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

        AllTables t = new("t");

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(t.Owner == _connInfo.Schema.ToUpper())
            .OrderBy(t.TableName);

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

        AllTabColumns atc = new("atc");

        ISqlBuilder sql =
            Select(
                atc.ColumnName,
                atc.DataType)
            .From(atc)
            .Where(
                And(
                    atc.Owner == _connInfo.Schema.ToUpper(),
                    atc.TableName == tableName.ToUpper()));

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
        AllTables t = new("t");

        ISqlBuilder sql =
            Select(Count(t.TableName))
            .From(t)
            .Where(
                And(
                    t.Owner == _connInfo.Schema.ToUpper(),
                    t.TableName == tableName.ToUpper()));

        int tableCount = Convert.ToInt32(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
