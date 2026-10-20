using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A window's <c>PARTITION BY</c> list, from <c>Sql.PartitionBy(...)</c>.
/// </summary>
public sealed class PartitionByClause : SqlPart
{
    private readonly SqlExpression[] _expressions;

    internal PartitionByClause(SqlExpression[] expressions)
    {
        CollectionGuard.ThrowIfEmpty(
            expressions, nameof(expressions), "PARTITION BY requires at least one expression.");

        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Partition} {Keywords.By} ")
        .AppendCsv(_expressions);

    /// <summary>
    /// Adds the window's <c>ORDER BY</c>: <c>PARTITION BY ... ORDER BY items</c>.
    /// </summary>
    /// <param name="orderByItems">The sort keys, each a column, an expression, or a
    /// <see cref="SortOrder"/>; at least one.</param>
    public PartitionByAndOrderBy OrderBy(params object[] orderByItems) =>
        new(this, OrderByClause.ParseInExpression(orderByItems));
}
