namespace SqlArtisan.Internal;

public sealed class DecodeFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly (SqlExpression, SqlExpression)[] _searchResultPairs;
    private readonly SqlExpression _default;

    internal DecodeFunction(
        SqlExpression expr,
        (SqlExpression, SqlExpression)[] searchResultPairs,
        SqlExpression @default)
    {
        _expr = expr;
        _searchResultPairs = searchResultPairs;
        _default = @default;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Decode)
            .OpenParenthesis()
            .Append(_expr);

        foreach ((SqlExpression, SqlExpression) pair in _searchResultPairs)
        {
            buffer.PrependComma(pair.Item1)
                .PrependComma(pair.Item2);
        }

        buffer.PrependComma(_default)
            .CloseParenthesis();
    }
}
