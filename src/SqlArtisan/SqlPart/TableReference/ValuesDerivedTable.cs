using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A table value constructor named as a derived-table source —
/// <c>(VALUES (…),(…)) "s" (c1, c2)</c> — for a MERGE <c>USING</c>. Build one
/// with <see cref="Sql.ValuesTable(string, string[], object[][])"/>; read its
/// columns by name through <see cref="Column(string)"/>.
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

    /// <summary>
    /// Returns the named column of this derived table, qualified by its alias.
    /// </summary>
    /// <param name="name">The column name to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(string name) => new(this, name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="source"/> — its column name, qualified by this alias.
    /// </summary>
    /// <param name="source">The source column whose name is re-qualified with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(DbColumn source) => new(this, source.Name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="alias"/> — a SELECT-list <c>.As(...)</c> — qualified by this alias.
    /// </summary>
    /// <param name="alias">The SELECT-list <c>.As(...)</c> alias to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.OpenParenthesis();
        _rows.Format(buffer);
        buffer.CloseParenthesis()
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
