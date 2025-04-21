namespace InlineSqlSharp;

public sealed class LikeCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractCondition
{
    private readonly LikeConditionCore _core = new(false, leftSide, rightSide);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
