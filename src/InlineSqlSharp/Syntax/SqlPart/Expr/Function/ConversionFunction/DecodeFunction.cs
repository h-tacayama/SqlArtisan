namespace InlineSqlSharp;

public sealed class DecodeFunction(
    AbstractExpr expr,
    (AbstractExpr, AbstractExpr)[] searchResultPairs,
    AbstractExpr @default) : AbstractExpr
{
    private readonly AbstractExpr _expr = expr;
    private readonly (AbstractExpr, AbstractExpr)[] _searchResultPairs =
        searchResultPairs;
    private readonly AbstractExpr _default = @default;

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.DECODE)
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
