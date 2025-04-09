namespace InlineSqlSharp;

public sealed class NotLikeCondition(IExpr leftSide, IExpr rightSide) : ICondition
{
    private readonly LikeConditionCore _core = new(true, leftSide, rightSide);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
