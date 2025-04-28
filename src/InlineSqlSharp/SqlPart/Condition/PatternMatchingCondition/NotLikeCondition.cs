namespace InlineSqlSharp;

public sealed class NotLikeCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly AbstractExpr _rightSide;

    internal NotLikeCondition(AbstractExpr leftSide, AbstractExpr rightSide)
    {
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_leftSide)
        .AppendSpace(Keywords.Not)
        .AppendSpace(Keywords.Like)
        .Append(_rightSide);
}
