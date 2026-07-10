using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// The base class for a typed table class: derive from it, name the table through
/// the constructor, and expose each column as a <see cref="DbColumn"/> property.
/// </summary>
public abstract class DbTableBase : TableReference
{
    private protected readonly string _tableAlias;

    /// <summary>
    /// Initializes a table whose name defaults to the subclass's type name, with the given alias.
    /// </summary>
    /// <param name="tableAlias">The table alias, or an empty string for none.</param>
    public DbTableBase(string tableAlias)
    {
        _tableAlias = tableAlias;
    }

    /// <summary>
    /// Initializes a table with an explicit name and alias.
    /// </summary>
    /// <param name="tableName">The table name as it appears in SQL.</param>
    /// <param name="tableAlias">The table alias, or an empty string for none.</param>
    public DbTableBase(string tableName, string tableAlias) : base(tableName)
    {
        _tableAlias = tableAlias;
    }

    internal override string CorrelationName => _tableAlias;

    // Whether this table carries an alias — read by the DML-target guard, since
    // aliasing an INSERT/UPDATE/DELETE target is rejected on SQL Server (ADR 0011).
    internal bool HasAlias => !string.IsNullOrEmpty(_tableAlias);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        base.Format(buffer);

        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.AppendSpace();
            buffer.EncloseInAliasQuotes(_tableAlias);
        }
    }

    // Renders the table as a DML target (INSERT / UPDATE / DELETE). The base
    // SELECT/FROM rendering separates the alias with a bare space; DML instead
    // uses the dialect's alias separator (` AS ` for most engines, ` ` for
    // Oracle), since several engines require AS where the FROM clause forbids it.
    internal void FormatAsDmlTarget(SqlBuildingBuffer buffer)
    {
        base.Format(buffer);

        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.AppendDmlTableAlias(_tableAlias);
        }
    }

    // Renders the reference a predicate targets by table (SQLite FTS5
    // `tbl MATCH ...`). FTS5 resolves the target as the hidden column named
    // after the table, so an aliased table must qualify it (`"a".tbl`) — a bare
    // quoted alias falls back to a string literal (no such column) and fails
    // with "unable to use function MATCH in the requested context".
    internal void FormatAsMatchTarget(SqlBuildingBuffer buffer)
    {
        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.EncloseInAliasQuotes(_tableAlias);
            buffer.Append('.');
        }

        base.Format(buffer);
    }
}
