namespace SqlArtisan.Internal;

public sealed class CeilFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal CeilFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendCeilFunctionName()
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
