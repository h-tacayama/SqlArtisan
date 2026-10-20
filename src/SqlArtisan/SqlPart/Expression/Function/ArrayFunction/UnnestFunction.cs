using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// PostgreSQL's <c>UNNEST(array, ...)</c>, from <c>Sql.Unnest(...)</c>; name it as a
/// table source with <see cref="AsTable(string)"/>.
/// </summary>
public sealed class UnnestFunction : SqlExpression
{
    private readonly SqlExpression[] _arrays;

    internal UnnestFunction(SqlExpression[] arrays)
    {
        _arrays = arrays;
    }

    /// <summary>
    /// Names this <c>UNNEST</c> call as a derived-table source for a <c>FROM</c>. One scalar
    /// array's column is named <paramref name="alias"/>; a composite-typed array's take its
    /// field names; with several arrays, read the scalar ones through <c>Asterisk</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        return new(this, alias, null);
    }

    /// <summary>
    /// Names this <c>UNNEST</c> call as a derived-table source with named result columns —
    /// <c>UNNEST(arrays) "alias" (col1, col2)</c>, one per array, or per field of a
    /// composite-typed array. Read them with <c>Column(name)</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <param name="columns">The result column names, in array order; at least one.</param>
    /// <returns>An <see cref="UnnestDerivedTable"/> naming this call.</returns>
    public UnnestDerivedTable AsTable(string alias, params string[] columns)
    {
        StringGuard.ThrowIfNullOrEmpty(alias, "A derived table requires an alias.");
        CollectionGuard.ThrowIfEmpty(
            columns, nameof(columns), "An UNNEST column alias list requires at least one column.");

        foreach (string column in columns)
        {
            StringGuard.ThrowIfNullOrWhiteSpace(
                column, "An UNNEST column alias list requires a name for every column.");
        }

        if (CommonTableExpression.HasDuplicateName(columns))
        {
            throw new ArgumentException(
                "An UNNEST column alias list requires a distinct name for every column.");
        }

        // No cap at the array count: a composite-typed array yields a column per
        // field, which the list may name (PostgreSQL 16.15, #664).
        return new(this, alias, columns);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Unnest)
        .OpenParenthesis()
        .AppendCsv(_arrays)
        .CloseParenthesis();
}
