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

    internal SubqueryDerivedTable(ISubquery subquery, string name) : base(name)
    {
        _subquery = subquery;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The name renders bare, while a string <c>.As("x")</c> alias renders quoted: read such an
    /// alias back with <see cref="Column(ExpressionAlias)"/>. By name it misses on Oracle when the
    /// alias has a lower-case letter, and on PostgreSQL when it has an upper-case one.
    /// </remarks>
    public DbColumn Column(string name) => new(this, name);

    /// <inheritdoc/>
    public DbColumn Column(DbColumn source) => new(this, source.Name, source.QuoteName);

    /// <inheritdoc/>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name, alias.QuoteAlias);

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .EncloseInAliasQuotes(_name);
}
