namespace InlineSqlSharp;

public sealed class RegexpLikeCondition(
    AbstractExpr source,
    AbstractExpr pattern,
    RegexpOptions? options = null) : AbstractCondition
{
    private readonly AbstractExpr _source = source;
    private readonly AbstractExpr _pattern = pattern;
    private readonly RegexpOptionsValue? _options = options?.ToValue();

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.REGEXP_LIKE)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_pattern)
        .PrependCommaIfNotNull(_options)
        .CloseParenthesis();
}
