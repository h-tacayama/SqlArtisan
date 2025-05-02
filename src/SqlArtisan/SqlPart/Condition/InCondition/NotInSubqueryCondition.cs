namespace SqlArtisan;

public sealed class NotInSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlPartAgent _subquery;

    internal NotInSubqueryCondition(SqlExpression leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = new(subquey.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.In} ")
        .EncloseInParentheses(_subquery);
}
