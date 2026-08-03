namespace SqlArtisan.Internal;

public sealed class UnnestFunction : SqlExpression
{
    private readonly SqlExpression[] _arrays;

    internal UnnestFunction(SqlExpression[] arrays)
    {
        _arrays = arrays;
    }

    /// <summary>
    /// Names this <c>UNNEST</c> call as a derived-table source —
    /// <c>UNNEST(array) "alias"</c> — for a <c>FROM</c>. A single array's
    /// result column is named <paramref name="alias"/>, read via
    /// <c>Column(alias)</c>; more than one array leaves the columns unnamed,
    /// read via <c>Asterisk</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        return new(this, alias, null);
    }

    /// <summary>
    /// Names this <c>UNNEST</c> call as a derived-table source with named
    /// result columns — <c>UNNEST(arrays) "alias" (col1, col2)</c>, at most
    /// one per unnested array. Read them with <c>Column(name)</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <param name="columns">The result column names, in array order; at least one, and no more than there are arrays.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias, params string[] columns)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        CollectionGuard.ThrowIfEmpty(
            columns, "An UNNEST column alias list requires at least one column.");

        if (columns.Length > _arrays.Length)
        {
            throw new ArgumentException(
                "An UNNEST column alias list must not name more columns than there are "
                    + $"unnested arrays; UNNEST has {_arrays.Length} array(s), but the column "
                    + $"list has {columns.Length}.");
        }

        return new(this, alias, columns);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Unnest)
        .OpenParenthesis()
        .AppendCsv(_arrays)
        .CloseParenthesis();
}
