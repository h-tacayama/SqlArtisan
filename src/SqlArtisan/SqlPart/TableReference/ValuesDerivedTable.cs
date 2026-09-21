using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A table value constructor named as a derived-table source —
/// <c>(VALUES (…),(…)) "s" (c1, c2)</c> — in <c>FROM</c>, a join, or a MERGE
/// <c>USING</c>. Build one with <see cref="Sql.Values(string, string[], object[][])"/>;
/// read its columns by name through <see cref="Column(string)"/>.
/// </summary>
public sealed class ValuesDerivedTable : DerivedTableBase, IColumnAccessor
{
    private readonly string[] _columns;
    private readonly InsertValuesClause _rows;

    internal ValuesDerivedTable(string name, string[] columns, InsertValuesClause rows)
        : base(name)
    {
        _columns = columns;
        _rows = rows;
    }

    /// <inheritdoc/>
    public DbColumn Column(string name) => new(this, name);

    // The explicit column list renders its names bare, so a reference must
    // render bare too — carrying a select item's quoting here would split one
    // identifier in two on a case-folding engine (#165).
    /// <inheritdoc/>
    public DbColumn Column(DbColumn source) => new(this, source.Name);

    /// <inheritdoc/>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.EncloseInParentheses(_rows)
            .AppendSpace()
            .EncloseInAliasQuotes(_name)
            .Append(" (")
            .Append(_columns[0]);

        for (int i = 1; i < _columns.Length; i++)
        {
            buffer.Append(", ").Append(_columns[i]);
        }

        buffer.CloseParenthesis();
    }
}
