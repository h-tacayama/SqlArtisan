namespace SqlArtisan.Internal;

public sealed class LnFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal LnFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Ln)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
