namespace InlineSqlSharp;

public sealed class RegexpLikeCondition(
    CharacterExpr source,
    CharacterExpr pattern,
    RegexpOptions? options = null) : ICondition
{
    private readonly CharacterExpr _source = source;
    private readonly CharacterExpr _pattern = pattern;
    private readonly RegexpOptionsValue? _options = options?.ToValue();

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.REGEXP_LIKE)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_pattern)
        .PrependCommaIfNotNull(_options)
        .CloseParenthesis();
}
