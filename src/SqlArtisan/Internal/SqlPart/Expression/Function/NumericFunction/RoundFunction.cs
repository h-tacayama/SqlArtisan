namespace SqlArtisan.Internal;

public sealed class RoundFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression? _decimals;

    internal RoundFunction(SqlExpression expr, SqlExpression? decimals = null)
    {
        _expr = expr;
        _decimals = decimals;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Round)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_decimals)
        .CloseParenthesis();
}
