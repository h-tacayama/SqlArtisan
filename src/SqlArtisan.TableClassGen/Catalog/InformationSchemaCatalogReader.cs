using System.Data;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.TableClassGen;

// Reads table metadata from the SQL-standard information_schema, shared by every
// engine that exposes it; the dialect is resolved from the connection, so the
// emitted catalog queries are dialected automatically. Oracle and SQLite differ.
internal sealed class InformationSchemaCatalogReader(
    DbConnectionInfo connInfo,
    bool lowercaseNames) : ICatalogReader
{
    private readonly DbConnectionInfo _connInfo = connInfo;
    private readonly bool _lowercaseNames = lowercaseNames;

    public IReadOnlyList<CatalogTable> GetAllTables()
    {
        using IDbConnection conn = _connInfo.OpenConnection();

        InformationSchemaTables t = new();

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(
                t.TableSchema == _connInfo.Schema
                & t.TableType == "BASE TABLE")
            .OrderBy(t.TableName);

        List<CatalogTable> tables = [];

        List<string> tableNames = [];
        using (IDataReader reader = conn.ExecuteReader(sql))
        {
            while (reader.Read())
            {
                tableNames.Add(reader.GetString(0));
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

    // The stored name the catalog returns, not the caller's spelling, is what a full
    // run emits: under a case-insensitive collation `--tables ORDERS` finds `Orders`.
    public bool TryGetTable(string tableName, out CatalogTable? table)
    {
        using IDbConnection conn = _connInfo.OpenConnection();

        table = null;

        return StoredTableName(conn, tableName) is { } storedName
            && TryGetTable(conn, storedName, out table);
    }

    // tableName is the catalog's stored name, reused verbatim as the re-lookup
    // key; lowercasing is applied only to the emitted names, so a case-sensitive
    // collation cannot drop a mixed-case table on re-lookup.
    private bool TryGetTable(IDbConnection conn, string tableName, out CatalogTable? table)
    {
        InformationSchemaColumns c = new();

        ISqlBuilder sql2 =
            Select(
                c.ColumnName,
                c.DataType,
                c.IsNullable,
                c.ColumnDefault)
            .From(c)
            .Where(
                c.TableSchema == _connInfo.Schema
                & c.TableName == tableName)
            .OrderBy(c.OrdinalPosition);

        ColumnIndexInfo indexes =
            new CatalogColumnIndexReader(_connInfo.Dbms, _connInfo.Schema)
                .Read(conn, tableName);

        List<CatalogColumn> columns = [];

        using (IDataReader reader = conn.ExecuteReader(sql2))
        {
            while (reader.Read())
            {
                string catalogName = reader.GetString(0);
                string dataType = reader.GetString(1);
                columns.Add(new CatalogColumn(
                    Normalize(catalogName),
                    dataType,
                    isNullable: ReadIsNullable(reader, 2),
                    hasDefault: ReadHasDefault(reader, 3, _connInfo.Dbms),
                    isIndexed: indexes.IsIndexed(catalogName),
                    dbms: _connInfo.Dbms));
            }
        }

        // A column-less table (PostgreSQL allows one) gets its class: skipped, the run
        // exited 0 short a table. One with a column the user cannot see is refused.
        if (LiveColumnCount(conn, tableName) is { } live
            ? columns.Count != live
            : columns.Count == 0)
        {
            throw new CommandLineException(_connInfo.NoVisibleColumnsMessage(tableName));
        }

        table = new CatalogTable(Normalize(tableName), columns, _connInfo.Schema);
        return true;
    }

    // PostgreSQL lists a table on any privilege but each column only on a privilege
    // over it, and pg_attribute counts the columns whatever the grants. MySQL and SQL
    // Server have no such count (null), and reject a table with no columns outright.
    private long? LiveColumnCount(IDbConnection conn, string tableName)
    {
        if (_connInfo.Dbms != Dbms.PostgreSql)
        {
            return null;
        }

        using IDbCommand command = conn.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relname = @table_name AND n.nspname = @schema_name
                AND a.attnum > 0 AND NOT a.attisdropped
            """;
        CatalogCommand.AddParameter(command, "@schema_name", _connInfo.Schema);
        CatalogCommand.AddParameter(command, "@table_name", tableName);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static bool? ReadIsNullable(IDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : string.Equals(reader.GetString(ordinal), "YES", StringComparison.OrdinalIgnoreCase);

    // An absent default stays unknown (an identity column reports none either); a
    // stored DEFAULT NULL is no default. Not on MySQL, which reports a string
    // default 'NULL' as the same bare text (MySqlTableClassGenTests pins it).
    private static bool? ReadHasDefault(IDataReader reader, int ordinal, Dbms dbms) =>
        reader.IsDBNull(ordinal) ? null
        : dbms != Dbms.MySql && DefaultExpression.IsNull(reader.GetString(ordinal)) ? false
        : true;

    private string Normalize(string name) => _lowercaseNames ? name.ToLowerInvariant() : name;

    private string? StoredTableName(IDbConnection conn, string tableName)
    {
        InformationSchemaTables t = new();

        ISqlBuilder sql =
            Select(t.TableName)
            .From(t)
            .Where(
                t.TableSchema == _connInfo.Schema
                & t.TableName == tableName
                & t.TableType == "BASE TABLE");

        return conn.ExecuteScalar(sql) as string;
    }
}
