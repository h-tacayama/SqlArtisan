namespace SqlArtisan.Internal;

public sealed class PositionFunction : SqlExpression
{
    private readonly SqlExpression _substring;
    private readonly SqlExpression _source;

    internal PositionFunction(SqlExpression substring, SqlExpression source)
    {
        _substring = substring;
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Position)
        .OpenParenthesis()
        .Append(_substring)
        .EncloseInSpaces(Keywords.In)
        .Append(_source)
        .CloseParenthesis();
}
