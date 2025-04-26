namespace InlineSqlSharp;

public sealed class InCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly AbstractExpr[] _expressions;

    internal InCondition(AbstractExpr leftSide, AbstractExpr[] expressions)
    {
        _leftSide = leftSide;
        _expressions = expressions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.IN)
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
