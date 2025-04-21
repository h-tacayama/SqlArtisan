namespace InlineSqlSharp;

public sealed class AddMonthsFunction : AbstractExpr
{
    private readonly BinaryFunctionCore _core;

    internal AddMonthsFunction(AbstractExpr dateTime, AbstractExpr months)
    {
        _core = new(Keywords.ADD_MONTHS, dateTime, months);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
