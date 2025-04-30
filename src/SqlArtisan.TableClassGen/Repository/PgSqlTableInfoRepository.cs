using System.Data;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.TableClassGen;

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

        InformationSchemaTables t = new("t");

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(
                t.TableSchema == _connInfo.Schema
                & t.TableType == "BASE TABLE")
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

        InformationSchemaColumns c = new("c");

        ISqlBuilder sql2 =
            Select(
                c.ColumnName,
                c.DataType)
            .From(c)
            .Where(
                c.TableSchema == _connInfo.Schema
                & c.TableName == tableName)
            .OrderBy(c.OrdinalPosition);

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
        InformationSchemaTables t = new("t");

        ISqlBuilder sql =
            Select(Count(t.TableName))
            .From(t)
            .Where(
                t.TableSchema == _connInfo.Schema
                & t.TableName == tableName
                & t.TableType == "BASE TABLE");

        long tableCount = Convert.ToInt64(conn.ExecuteScalar(sql));
        return tableCount > 0;
    }
}
