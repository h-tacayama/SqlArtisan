namespace InlineSqlSharp;

internal sealed class LikeConditionCore(
    bool isNot,
    AbstractExpr leftSide,
    AbstractExpr rightSide)
{
    private readonly bool _isNot = isNot;
    private readonly AbstractExpr _leftSide = leftSide;
    private readonly AbstractExpr _rightSide = rightSide;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.LIKE)
        .Append(_rightSide);
}
