namespace SqlArtisan.Internal;

public sealed class ReplaceFunction : SqlExpression
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _search;
    private readonly SqlExpression _replacement;

    internal ReplaceFunction(
        SqlExpression source,
        SqlExpression search,
        SqlExpression replacement)
    {

        _source = source;
        _search = search;
        _replacement = replacement;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Replace)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_search)
        .PrependComma(_replacement)
        .CloseParenthesis();
}
