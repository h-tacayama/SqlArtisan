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
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.NOT)
        .AppendSpace(Keywords.IN)
        .EncloseInParentheses(_subquery);
}
