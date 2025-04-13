namespace InlineSqlSharp;

internal sealed class DecodeFunctionCore(
    object expr,
    (object, object)[] searchResultPairs,
    object @default)
{
    private readonly object _expr = expr;
    private readonly (object, object)[] _searchResultPairs = searchResultPairs;
    private readonly object _default = @default;

    internal void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.DECODE)
            .OpenParenthesis()
            .Append(ExprOrBindValue(_expr));

        foreach ((object, object) pair in _searchResultPairs)
        {
            buffer.PrependComma(ExprOrBindValue(pair.Item1))
                .PrependComma(ExprOrBindValue(pair.Item2));
        }

        buffer.PrependComma(ExprOrBindValue(_default))
            .CloseParenthesis();
    }

    private IExpr ExprOrBindValue(object value)
    {
        if (value is IExpr expr)
        {
            return expr;
        }
        else
        {
            return BindValueFactory.CreateOrException(value);
        }
    }
}
