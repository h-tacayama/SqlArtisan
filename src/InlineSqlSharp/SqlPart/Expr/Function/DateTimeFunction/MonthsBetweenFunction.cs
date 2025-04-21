namespace InlineSqlSharp;

public sealed class MonthsBetweenFunction : AbstractExpr
{
    private readonly BinaryFunctionCore _core;

    internal MonthsBetweenFunction(AbstractExpr date1, AbstractExpr date2)
    {
        _core = new(Keywords.MONTHS_BETWEEN, date1, date2);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
