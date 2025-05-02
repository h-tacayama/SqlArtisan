namespace SqlArtisan;

public sealed class LengthBFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal LengthBFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LengthB)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
