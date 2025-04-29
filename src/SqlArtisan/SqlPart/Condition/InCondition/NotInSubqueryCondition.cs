namespace InlineSqlSharp;

public sealed class NotInSubqueryCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly SqlPartAgent _subquery;

    internal NotInSubqueryCondition(AbstractExpr leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = new(subquey.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.In} ")
        .EncloseInParentheses(_subquery);
}
