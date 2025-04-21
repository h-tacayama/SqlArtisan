namespace InlineSqlSharp;

public sealed class LastDayFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal LastDayFunction(AbstractExpr date)
    {
        _core = new(Keywords.LAST_DAY, date);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
