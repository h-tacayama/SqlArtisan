namespace SqlArtisan.Internal;

public sealed class SqrtFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal SqrtFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Sqrt)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
