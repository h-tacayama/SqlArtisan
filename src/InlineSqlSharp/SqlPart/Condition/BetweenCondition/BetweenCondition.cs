namespace InlineSqlSharp;

public sealed class BetweenCondition : AbstractCondition
{
    private readonly AbstractExpr _leftSide;
    private readonly AbstractExpr _rightSide1;
    private readonly AbstractExpr _rightSide2;

    internal BetweenCondition(
        AbstractExpr leftSide,
        AbstractExpr rightSide1,
        AbstractExpr rightSide2)
    {
        _leftSide = leftSide;
        _rightSide1 = rightSide1;
        _rightSide2 = rightSide2;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .Append($" {Keywords.Between} ")
        .Append(_rightSide1)
        .Append($" {Keywords.And} ")
        .Append(_rightSide2);
}
