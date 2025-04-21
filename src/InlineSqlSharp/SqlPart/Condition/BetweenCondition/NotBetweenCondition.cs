namespace InlineSqlSharp;

public sealed class NotBetweenCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide1,
    AbstractExpr rightSide2) : AbstractCondition
{
    private readonly BetweenConditionCore _core = new(
        true,
        leftSide,
        rightSide1,
        rightSide2);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
