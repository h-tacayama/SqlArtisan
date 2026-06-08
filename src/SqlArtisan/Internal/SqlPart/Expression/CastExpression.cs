namespace SqlArtisan.Internal;

public sealed class CastExpression : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly string _type;

    internal CastExpression(SqlExpression expr, string type)
    {
        _expr = expr;
        _type = type;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Cast)
        .OpenParenthesis()
        .Append(_expr)
        .EncloseInSpaces(Keywords.As)
        .Append(_type)
        .CloseParenthesis();
}
