namespace InlineSqlSharp;

public sealed class DateTimeDecodeFunction(
    object expr,
    (object, object)[] searchResultPairs,
    DateTimeExpr @default) : DateTimeExpr
{
    private readonly DecodeFunctionCore<DateTimeExpr> _core =
        new(expr, searchResultPairs, @default);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
