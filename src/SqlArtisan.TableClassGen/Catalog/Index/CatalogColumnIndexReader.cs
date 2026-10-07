using System.Data;
using MySqlConnector;

namespace SqlArtisan.TableClassGen;

// No engine records index key order in information_schema portably (MySQL alone
// exposes it there), so each dialect gets its own catalog query sharing one
// shape: leading column name, plus the expression text for an expression key.
internal sealed class CatalogColumnIndexReader(Dbms dbms, string schema)
    : IColumnIndexReader
{
    public ColumnIndexInfo Read(IDbConnection conn, string tableName)
    {
        List<string> leadingColumns = [];
        List<string> expressionTexts = [];
        List<string> undecidedLeadingColumns = [];

        // Older MySQL lacks STATISTICS.EXPRESSION (before 8.0.13) or IS_VISIBLE (before
        // 8.0); only that error steps down to the next query, never a real failure.
        foreach (string sql in LeadingKeyQueries())
        {
            try
            {
                ReadLeadingKeys(
                    conn, tableName, sql,
                    leadingColumns, expressionTexts, undecidedLeadingColumns);
                break;
            }
            catch (MySqlException ex)
                when (ex.ErrorCode == MySqlErrorCode.BadFieldError && sql != MySql57Query)
            {
            }
        }

        return dbms == Dbms.Oracle && HasFunctionBasedIndex(conn, tableName)
            ? ColumnIndexInfo.Unknown
            : new ColumnIndexInfo(leadingColumns, expressionTexts, undecidedLeadingColumns);
    }

    private void ReadLeadingKeys(
        IDbConnection conn,
        string tableName,
        string sql,
        List<string> leadingColumns,
        List<string> expressionTexts,
        List<string> undecidedLeadingColumns)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText = sql;
        CatalogCommand.AddParameter(command, ParameterName(SchemaParameter), CatalogName(schema));
        CatalogCommand.AddParameter(command, ParameterName(TableParameter), CatalogName(tableName));

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            // A row can carry both: PostgreSQL's mixed index (a column, then an
            // expression), or a SQL Server computed column with its definition.
            if (!reader.IsDBNull(1))
            {
                expressionTexts.Add(reader.GetString(1));
            }

            if (!reader.IsDBNull(0))
            {
                bool undecided = !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetValue(2));
                (undecided ? undecidedLeadingColumns : leadingColumns).Add(reader.GetString(0));
            }
        }
    }

    // An INVISIBLE index (8.0+) is maintained but never chosen, so it serves no query;
    // the 8.0.0 to 8.0.12 step keeps that filter, and only 5.7, which has no
    // invisible index, drops it.
    private const string MySql80Query =
        """
        SELECT COLUMN_NAME, NULL, 0
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = @table_name AND SEQ_IN_INDEX = 1
            AND IS_VISIBLE = 'YES'
        """;

    private const string MySql57Query =
        """
        SELECT COLUMN_NAME, NULL, 0
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = @table_name AND SEQ_IN_INDEX = 1
        """;

    private IEnumerable<string> LeadingKeyQueries() =>
        dbms == Dbms.MySql ? [LeadingKeyQuery(), MySql80Query, MySql57Query] : [LeadingKeyQuery()];

    // Each row is a leading column name, an expression text, and whether the lead
    // decides nothing for a bare predicate: a partial index, or on PostgreSQL one that
    // is neither B-tree nor hash (a trigram GIN index serves LIKE '%x%', #645).
    private string LeadingKeyQuery() => dbms switch
    {
        Dbms.MySql =>
            """
            SELECT COLUMN_NAME, EXPRESSION, 0
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = @table_name AND SEQ_IN_INDEX = 1
                AND IS_VISIBLE = 'YES'
            """,

        // indkey is 0 at a subscript whose key is an expression, and no attribute has
        // attnum 0, so the join drops exactly those rows to a null column name.
        Dbms.PostgreSql =>
            """
            SELECT a.attname, pg_get_expr(i.indexprs, i.indrelid),
                i.indpred IS NOT NULL OR am.amname NOT IN ('btree', 'hash')
            FROM pg_index i
            JOIN pg_class c ON c.oid = i.indrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_class ic ON ic.oid = i.indexrelid
            JOIN pg_am am ON am.oid = ic.relam
            LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = i.indkey[0]
            WHERE c.relname = @table_name AND n.nspname = @schema_name AND i.indisvalid
            """,

        // T-SQL indexes no expression directly; the equivalent is an index whose
        // leading key is a computed column, whose definition names the real columns.
        Dbms.SqlServer =>
            """
            SELECT c.name, cc.definition, i.has_filter
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id
                AND ic.index_id = i.index_id AND ic.key_ordinal = 1
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id
                AND cc.column_id = c.column_id
            JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.name = @table_name AND s.name = @schema_name AND i.is_disabled = 0
                AND i.is_hypothetical = 0
            """,

        // COLUMN_EXPRESSION is a LONG, so a function-based index disqualifies the
        // table below instead; a partitioned index's 'N/A' status stays in.
        Dbms.Oracle =>
            """
            SELECT ic.COLUMN_NAME, NULL, 0
            FROM ALL_IND_COLUMNS ic
            JOIN ALL_INDEXES i ON i.OWNER = ic.INDEX_OWNER AND i.INDEX_NAME = ic.INDEX_NAME
            WHERE ic.TABLE_OWNER = :schema_name AND ic.TABLE_NAME = :table_name
                AND ic.COLUMN_POSITION = 1 AND i.STATUS != 'UNUSABLE'
                AND i.VISIBILITY = 'VISIBLE'
            """,

        _ => throw new ArgumentOutOfRangeException(nameof(dbms)),
    };

    private bool HasFunctionBasedIndex(IDbConnection conn, string tableName)
    {
        using IDbCommand command = conn.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM ALL_INDEXES
            WHERE TABLE_OWNER = :schema_name AND TABLE_NAME = :table_name
                AND INDEX_TYPE LIKE 'FUNCTION-BASED%' AND STATUS != 'UNUSABLE'
                AND VISIBILITY = 'VISIBLE'
            """;
        CatalogCommand.AddParameter(command, ParameterName(SchemaParameter), CatalogName(schema));
        CatalogCommand.AddParameter(command, ParameterName(TableParameter), CatalogName(tableName));

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    // Oracle stores unquoted identifiers folded to upper case, and the caller may
    // hand down a name already lowercased for emission — matching the rest of the
    // Oracle reader, which compares against ToUpperInvariant() throughout.
    private string CatalogName(string name) =>
        dbms == Dbms.Oracle ? name.ToUpperInvariant() : name;

    // Suffixed because Oracle rejects a bind variable named after a reserved word:
    // a bare ":table" fails with ORA-01745, verified against a live engine.
    private const string SchemaParameter = "schema_name";

    private const string TableParameter = "table_name";

    private string ParameterName(string name) =>
        dbms == Dbms.Oracle ? $":{name}" : $"@{name}";
}
