using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A reference to a table column, rendered <c>alias.column</c> (or bare <c>column</c>
/// when the owning table has no correlation name). Expose one per column from a
/// <see cref="DbTableBase"/> subclass.
/// </summary>
/// <param name="owner">The table, CTE, or derived table that owns this column.</param>
/// <param name="columnName">The column name as it appears in SQL.</param>
public sealed class DbColumn(TableReference owner, string columnName) : SqlExpression
{
    internal TableReference Owner => owner;
    internal string Name => columnName;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        string correlationName = owner.CorrelationName;

        if (!string.IsNullOrEmpty(correlationName))
        {
            buffer.EncloseInAliasQuotes(correlationName);
            buffer.Append('.');
        }

        buffer.Append(Name);
    }

    // Renders the bare column name with no table-alias qualifier. DML contexts
    // that name a target column — the INSERT column list, the ON CONFLICT
    // target, and SET / DO UPDATE SET left sides — must stay unqualified;
    // PostgreSQL rejects an alias-qualified column in those positions.
    internal void FormatUnqualified(SqlBuildingBuffer buffer) =>
        buffer.Append(Name);
}
