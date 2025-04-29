namespace InlineSqlSharp;

public sealed class DecodeFunction : AbstractExpr
{
    private readonly AbstractExpr _expr;
    private readonly (AbstractExpr, AbstractExpr)[] _searchResultPairs;
    private readonly AbstractExpr _default;

    internal DecodeFunction(
        AbstractExpr expr,
        (AbstractExpr, AbstractExpr)[] searchResultPairs,
        AbstractExpr @default)
    {
        _expr = expr;
        _searchResultPairs = searchResultPairs;
        _default = @default;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Decode)
            .OpenParenthesis()
            .Append(_expr);

        foreach ((AbstractExpr, AbstractExpr) pair in _searchResultPairs)
        {
            buffer.PrependComma(pair.Item1)
                .PrependComma(pair.Item2);
        }

        buffer.PrependComma(_default)
            .CloseParenthesis();
    }
}
