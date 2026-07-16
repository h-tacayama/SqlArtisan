using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A subquery named as a derived-table source — <c>(SELECT …) "s"</c> — for a
/// <c>FROM</c>/<c>JOIN</c> or MERGE <c>USING</c>. Build one with
/// <see cref="ISubquery.AsTable(string)"/>; read its projected columns by name
/// through <see cref="Column(string)"/>.
/// </summary>
public sealed class SubqueryDerivedTable : DerivedTableBase, IColumnAccessor
{
    private readonly ISubquery _subquery;

    internal SubqueryDerivedTable(ISubquery subquery, string name) : base(name) =>
        _subquery = subquery;

    /// <summary>
    /// Returns the named column of this derived table, qualified by its alias.
    /// </summary>
    /// <param name="name">The column name to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(string name) => new(this, name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="source"/> — its column name, qualified by this alias. Use when the subquery projects the column unaliased.
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

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .EncloseInAliasQuotes(_name);
}
