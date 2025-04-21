namespace InlineSqlSharp;

internal sealed class InSubqueryConditionCore(
    bool isNot,
    AbstractExpr leftSide,
    ISubquery subquey)
{
    private readonly bool _isNot = isNot;
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly SqlPartAgent _subquery = new(subquey.FormatSql);

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.IN)
        .EncloseInParentheses(_subquery);
}
