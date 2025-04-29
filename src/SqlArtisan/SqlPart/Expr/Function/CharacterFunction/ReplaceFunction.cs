namespace SqlArtisan;

public sealed class ReplaceFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart _search;
    private readonly AbstractSqlPart _replacement;

    internal ReplaceFunction(
        AbstractExpr source,
        AbstractExpr search,
        AbstractExpr replacement)
    {

        _source = source;
        _search = search;
        _replacement = replacement;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Replace)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_search)
        .PrependComma(_replacement)
        .CloseParenthesis();
}
