namespace InlineSqlSharp;

public sealed class NumericDecodeFunction<TSearchExpr>(
    TSearchExpr expr,
    (TSearchExpr, NumericExpr)[] searchResultPairs,
    NumericExpr @default) : NumericExpr
    where TSearchExpr : IDataTypeExpr
{
    private readonly DecodeFunctionCore<TSearchExpr, NumericExpr> _core =
        new(expr, searchResultPairs, @default);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
