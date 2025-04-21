namespace InlineSqlSharp;

internal sealed class BetweenConditionCore(
    bool isNot,
    AbstractExpr leftSide,
    AbstractExpr rightSide1,
    AbstractExpr rightSide2)
{
    private readonly bool _isNot = isNot;
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr _rightSide1 = rightSide1;
    private readonly AbstractExpr _rightSide2 = rightSide2;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.BETWEEN)
        .AppendSpace(_rightSide1)
        .AppendSpace(Keywords.AND)
        .Append(_rightSide2);
}
