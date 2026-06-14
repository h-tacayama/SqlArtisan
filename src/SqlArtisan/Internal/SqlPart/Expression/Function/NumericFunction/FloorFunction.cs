namespace SqlArtisan.Internal;

public sealed class FloorFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal FloorFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Floor)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
