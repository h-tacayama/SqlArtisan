namespace SqlArtisan;

public sealed class InSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlPartAgent _subquery;

    internal InSubqueryCondition(SqlExpression leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = new(subquey.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.In} ")
        .EncloseInParentheses(_subquery);
}
