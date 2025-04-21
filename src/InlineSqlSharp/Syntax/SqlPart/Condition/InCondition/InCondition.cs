namespace InlineSqlSharp;

public sealed class InCondition(
    AbstractExpr leftSide,
    AbstractExpr[] expressions) : AbstractCondition
{
    private readonly InConditionCore _core = new(
        false,
        leftSide,
        expressions);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
