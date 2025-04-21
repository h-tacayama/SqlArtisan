namespace InlineSqlSharp;

public sealed class NotInCondition(
    AbstractExpr leftSide,
    AbstractExpr[] expressions) : AbstractCondition
{
    private readonly InConditionCore _core = new(
        true,
        leftSide,
        expressions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
