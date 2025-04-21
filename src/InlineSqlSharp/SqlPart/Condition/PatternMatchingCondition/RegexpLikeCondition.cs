namespace InlineSqlSharp;

public sealed class RegexpLikeCondition : AbstractCondition
{
    private readonly AbstractExpr _source;
    private readonly AbstractExpr _pattern;
    private readonly RegexpOptionsValue? _options;

    internal RegexpLikeCondition(
        AbstractExpr source,
        AbstractExpr pattern,
        RegexpOptions? options = null)
    {
        _source = source;
        _pattern = pattern;
        _options = options?.ToValue();
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.REGEXP_LIKE)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_pattern)
        .PrependCommaIfNotNull(_options)
        .CloseParenthesis();
}
