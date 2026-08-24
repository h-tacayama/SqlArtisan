namespace SqlArtisan.Internal;

/// <summary>
/// The <c>WITHIN GROUP (ORDER BY ...)</c> clause of an ordered-set or string
/// aggregate (<c>LISTAGG</c>, SQL Server's <c>STRING_AGG</c>).
/// </summary>
internal sealed class WithinGroupClause : SqlPart
{
    private readonly OrderByClause _orderByClause;

    internal WithinGroupClause(OrderByClause orderByClause)
    {
        _orderByClause = orderByClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Within} {Keywords.Group}")
        .AppendSpace()
        .OpenParenthesis()
        .Append(_orderByClause)
        .CloseParenthesis();
}
