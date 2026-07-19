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
    /// <c>UNNEST(array) "alias"</c> — for a <c>FROM</c>. The single result
    /// column is also named <paramref name="alias"/>; read it with
    /// <c>Column(alias)</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias)
    {
        ArgumentException.ThrowIfNullOrEmpty(alias);
        return new(this, alias, null);
    }

    /// <summary>
    /// Names this <c>UNNEST</c> call as a derived-table source with named
    /// result columns — <c>UNNEST(arrays) "alias" (col1, col2)</c> — one
    /// column per unnested array. Read them with <c>Column(name)</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <param name="columns">The result column names, in array order; at least one.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias, params string[] columns)
    {
        ArgumentException.ThrowIfNullOrEmpty(alias);
        CollectionGuard.ThrowIfEmpty(
            columns, "An UNNEST column alias list requires at least one column.");
        return new(this, alias, columns);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Unnest)
        .OpenParenthesis()
        .AppendCsv(_arrays)
        .CloseParenthesis();
}
