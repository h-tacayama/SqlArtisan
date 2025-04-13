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
            .Append(_expr);

        foreach ((object, object) pair in _searchResultPairs)
        {
            buffer.PrependComma(pair.Item1)
                .PrependComma(pair.Item2);
        }

        buffer.PrependComma(_default)
            .CloseParenthesis();
    }
}
