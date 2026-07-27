using System.Data;
using System.Data.Common;

namespace SqlArtisan.TableClassGen;

// No engine records index key order in information_schema in a portable way — MySQL
// alone exposes it there — so each dialect gets its own catalog query. The shape is
// shared: leading column name, plus the expression text when the key is an
// expression rather than a column.
internal sealed class CatalogColumnIndexRepository(DbmsType dbmsType, string schema)
    : IColumnIndexRepository
{
    public ColumnIndexInfo Read(IDbConnection conn, string tableName)
    {
        List<string> leadingColumns = [];
        List<string> expressionTexts = [];

        try
        {
            ReadLeadingKeys(conn, tableName, LeadingKeyQuery(), leadingColumns, expressionTexts);
        }
        catch (DbException) when (dbmsType == DbmsType.MySql)
        {
            // STATISTICS.EXPRESSION arrived with functional indexes in 8.0.13, so a
            // server that rejects the column has no expression index to miss.
            ReadLeadingKeys(conn, tableName, MySqlLegacyQuery, leadingColumns, expressionTexts);
        }

        return dbmsType == DbmsType.Oracle && HasFunctionBasedIndex(conn, tableName)
            ? ColumnIndexInfo.Unknown
            : new ColumnIndexInfo(leadingColumns, expressionTexts);
    }

    private void ReadLeadingKeys(
        IDbConnection conn,
        string tableName,
        string sql,
        List<string> leadingColumns,
        List<string> expressionTexts)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, ParameterName("schema"), schema);
        AddParameter(command, ParameterName("table"), tableName);

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(1))
            {
                expressionTexts.Add(reader.GetString(1));
            }
            else if (!reader.IsDBNull(0))
            {
                leadingColumns.Add(reader.GetString(0));
            }
        }
    }

    private const string MySqlLegacyQuery =
        "SELECT COLUMN_NAME, NULL FROM information_schema.STATISTICS "
        + "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND SEQ_IN_INDEX = 1";

    // Each returns (leading column name, index expression text) with exactly one of
    // the two non-null per row.
    private string LeadingKeyQuery() => dbmsType switch
    {
        DbmsType.MySql =>
            "SELECT COLUMN_NAME, EXPRESSION FROM information_schema.STATISTICS "
            + "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND SEQ_IN_INDEX = 1",

        // indkey is 0 at a subscript whose key is an expression, and no attribute has
        // attnum 0, so the join drops exactly those rows to a null column name.
        DbmsType.PostgreSql =>
            "SELECT a.attname, pg_get_expr(i.indexprs, i.indrelid) "
            + "FROM pg_index i "
            + "JOIN pg_class c ON c.oid = i.indrelid "
            + "JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = i.indkey[0] "
            + "WHERE c.relname = @table AND n.nspname = @schema",

        // T-SQL indexes no expression directly; the equivalent is an index whose
        // leading key is a computed column, whose definition names the real columns.
        DbmsType.SqlServer =>
            "SELECT c.name, cc.definition "
            + "FROM sys.indexes i "
            + "JOIN sys.index_columns ic ON ic.object_id = i.object_id "
            + "AND ic.index_id = i.index_id AND ic.key_ordinal = 1 "
            + "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id "
            + "LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id "
            + "AND cc.column_id = c.column_id "
            + "JOIN sys.tables t ON t.object_id = i.object_id "
            + "JOIN sys.schemas s ON s.schema_id = t.schema_id "
            + "WHERE t.name = @table AND s.name = @schema",

        // COLUMN_EXPRESSION is a LONG, so nothing is read from it here; a
        // function-based index instead disqualifies the whole table below.
        DbmsType.Oracle =>
            "SELECT COLUMN_NAME, NULL FROM ALL_IND_COLUMNS "
            + "WHERE TABLE_OWNER = :schema AND TABLE_NAME = :table AND COLUMN_POSITION = 1",

        _ => throw new ArgumentOutOfRangeException(nameof(dbmsType)),
    };

    private bool HasFunctionBasedIndex(IDbConnection conn, string tableName)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM ALL_INDEXES "
            + "WHERE TABLE_OWNER = :schema AND TABLE_NAME = :table "
            + "AND INDEX_TYPE LIKE 'FUNCTION-BASED%'";
        AddParameter(command, ":schema", schema);
        AddParameter(command, ":table", tableName);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private string ParameterName(string name) =>
        dbmsType == DbmsType.Oracle ? $":{name}" : $"@{name}";

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
