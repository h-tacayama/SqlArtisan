namespace InlineSqlSharp;

public sealed class NotLikeCondition : AbstractCondition
{
    private readonly LikeConditionCore _core;

    internal NotLikeCondition(AbstractExpr leftSide, AbstractExpr rightSide)
    {
        _core = new(true, leftSide, rightSide);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
