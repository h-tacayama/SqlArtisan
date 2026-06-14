namespace SqlArtisan.Internal;

public sealed class CeilingFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal CeilingFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Ceiling)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
