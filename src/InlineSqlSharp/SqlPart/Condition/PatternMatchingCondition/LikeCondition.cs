namespace InlineSqlSharp;

public sealed class LikeCondition : AbstractCondition
{
    private readonly LikeConditionCore _core;

    internal LikeCondition(AbstractExpr leftSide, AbstractExpr rightSide)
    {
        _core = new(false, leftSide, rightSide);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
