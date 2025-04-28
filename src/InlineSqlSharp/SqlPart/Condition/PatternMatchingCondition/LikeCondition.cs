namespace InlineSqlSharp;

public sealed class LikeCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly AbstractExpr _rightSide;

    internal LikeCondition(AbstractExpr leftSide, AbstractExpr rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.Like)
        .Append(_rightSide);
}
