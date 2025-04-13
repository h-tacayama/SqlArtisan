namespace InlineSqlSharp;

public sealed class DateTimeDecodeFunction : DateTimeExpr
{
    private readonly DecodeFunctionCore _core;

    public DateTimeDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        DateTimeExpr @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public DateTimeDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        DateTime @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
