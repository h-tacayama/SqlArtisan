namespace InlineSqlSharp;

public sealed class LastDayFunction(AbstractExpr date) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LAST_DAY, date);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
