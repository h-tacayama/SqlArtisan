namespace SqlArtisan;

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
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.Like} ")
        .Append(_rightSide);
}
