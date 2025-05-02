namespace SqlArtisan;

public sealed class UpperFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal UpperFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Upper)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
