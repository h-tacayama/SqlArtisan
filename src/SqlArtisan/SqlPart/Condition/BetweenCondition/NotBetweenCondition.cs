namespace SqlArtisan;

public sealed class NotBetweenCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlExpression _rightSide1;
    private readonly SqlExpression _rightSide2;

    internal NotBetweenCondition(
        SqlExpression leftSide,
        SqlExpression rightSide1,
        SqlExpression rightSide2)
    {
        _leftSide = leftSide;
        _rightSide1 = rightSide1;
        _rightSide2 = rightSide2;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Not} {Keywords.Between} ")
        .Append(_rightSide1)
        .Append($" {Keywords.And} ")
        .Append(_rightSide2);
}
