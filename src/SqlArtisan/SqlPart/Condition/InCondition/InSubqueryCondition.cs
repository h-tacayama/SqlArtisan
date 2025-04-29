namespace SqlArtisan;

public sealed class InSubqueryCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly SqlPartAgent _subquery;

    internal InSubqueryCondition(AbstractExpr leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = new(subquey.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.In} ")
        .EncloseInParentheses(_subquery);
}
