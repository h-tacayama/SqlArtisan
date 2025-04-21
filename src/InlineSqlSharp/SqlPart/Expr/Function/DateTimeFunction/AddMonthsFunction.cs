namespace InlineSqlSharp;

public sealed class AddMonthsFunction(
    AbstractExpr dateTime,
    AbstractExpr months) : AbstractExpr
{
    private readonly BinaryFunctionCore _core =
        new(Keywords.ADD_MONTHS, dateTime, months);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
