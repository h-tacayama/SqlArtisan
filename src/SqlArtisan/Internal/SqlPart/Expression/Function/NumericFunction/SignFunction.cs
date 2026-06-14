namespace SqlArtisan.Internal;

public sealed class SignFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal SignFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Sign)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
