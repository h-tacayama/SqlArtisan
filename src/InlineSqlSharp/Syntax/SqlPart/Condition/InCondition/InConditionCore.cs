namespace InlineSqlSharp;

internal sealed class InConditionCore(
    bool isNot,
    AbstractExpr leftSide,
    AbstractExpr[] expressions)
{
    private readonly bool _isNot = isNot;
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr[] _expressions = expressions;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.IN)
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
