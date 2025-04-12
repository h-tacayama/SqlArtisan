namespace InlineSqlSharp;

internal sealed class DecodeFunctionCore<TSearchExpr, TResultExpr>(
    TSearchExpr expr,
    (TSearchExpr, TResultExpr)[] searchResultPairs,
    TResultExpr @default)
    where TSearchExpr : IDataTypeExpr
    where TResultExpr : IDataTypeExpr
{
    private readonly TSearchExpr _expr = expr;
    private readonly (TSearchExpr, TResultExpr)[] _searchResultPairs = searchResultPairs;
    private readonly TResultExpr _default = @default;

    internal void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.DECODE)
            .OpenParenthesis()
            .Append(_expr);

        foreach ((TSearchExpr, TResultExpr) pair in _searchResultPairs)
        {
            buffer.PrependComma(pair.Item1)
                .PrependComma(pair.Item2);
        }

        buffer.PrependComma(_default)
            .CloseParenthesis();
    }
}
