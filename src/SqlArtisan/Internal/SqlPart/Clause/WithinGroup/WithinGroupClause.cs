namespace SqlArtisan.Internal;

/// <summary>
/// The <c>WITHIN GROUP (ORDER BY ...)</c> clause of an ordered-set aggregate.
/// </summary>
public sealed class WithinGroupClause : SqlPart
{
    private readonly OrderByClause _orderByClause;

    internal WithinGroupClause(OrderByClause orderByClause)
    {
        _orderByClause = orderByClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.WithinGroup)
        .AppendSpace()
        .OpenParenthesis()
        .Append(_orderByClause)
        .CloseParenthesis();
}
