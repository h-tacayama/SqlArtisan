namespace SqlArtisan.Internal;

public sealed class ExpFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal ExpFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Exp)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
