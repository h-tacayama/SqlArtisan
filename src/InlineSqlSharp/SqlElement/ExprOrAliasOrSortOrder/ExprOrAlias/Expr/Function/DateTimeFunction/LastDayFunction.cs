namespace InlineSqlSharp;

public sealed class LastDayFunction(DateTimeExpr date) : DateTimeExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LAST_DAY, date);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
