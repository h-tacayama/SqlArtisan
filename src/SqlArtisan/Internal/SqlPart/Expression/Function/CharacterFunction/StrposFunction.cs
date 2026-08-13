namespace SqlArtisan.Internal;

public sealed class StrposFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _substring;

    internal StrposFunction(SqlExpression source, SqlExpression substring)
    {
        _source = source;
        _substring = substring;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Strpos)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_substring)
        .CloseParenthesis();
}
