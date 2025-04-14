using System.Data;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.TableClassGen;

internal sealed class PgSqlTableInfoRepository(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ITableInfoRepository
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<DbTableInfo> GetAllTables()
    {
        using IDbConnection conn = _connInfo.CreateConnection();
        conn.Open();

        information_schema_tables t = new("t");

        ISqlBuilder sql =
            SELECT(t.table_name)
            .FROM(t)
            .WHERE(
                AND(
                    t.table_schema == P(_connInfo.Schema),
                    t.table_type == L("BASE TABLE")))
            .ORDER_BY(t.table_name);

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

        information_schema_columns c = new("c");

        ISqlBuilder sql2 =
            SELECT(
                c.column_name,
                c.data_type)
            .FROM(c)
            .WHERE(
                AND(
                    c.table_schema == P(_connInfo.Schema),
                    c.table_name == P(tableName)))
            .ORDER_BY(c.ordinal_position);

        List<DbColumnInfo> columns = new();

        using (IDataReader reader = conn.ExecuteReader(sql2))
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

        table = new DbTableInfo(tableName, columns);
        return true;
    }

    private bool ExistsTable(IDbConnection conn, string tableName)
    {
        information_schema_tables t = new("t");

        ISqlBuilder sql =
            SELECT(COUNT(t.table_name))
            .FROM(t)
            .WHERE(
                AND(
                    t.table_schema == P(_connInfo.Schema),
                    t.table_name == P(tableName),
                    t.table_type == L("BASE TABLE")));

        long tableCount = Convert.ToInt64(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
