namespace InlineSqlSharp;

public sealed class DateTimeDecodeFunction<TSearchExpr>(
    TSearchExpr expr,
    (TSearchExpr, DateTimeExpr)[] searchResultPairs,
    DateTimeExpr @default) : DateTimeExpr
    where TSearchExpr : IExpr
{
    private readonly DecodeFunctionCore<TSearchExpr, DateTimeExpr> _core =
        new(expr, searchResultPairs, @default);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
