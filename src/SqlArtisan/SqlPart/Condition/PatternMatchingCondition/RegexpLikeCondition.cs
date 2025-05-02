namespace SqlArtisan;

public sealed class RegexpLikeCondition : SqlCondition
{
    private readonly SqlExpression _source;
    private readonly SqlExpression _pattern;
    private readonly RegexpOptionsValue? _options;

    internal RegexpLikeCondition(
        SqlExpression source,
        SqlExpression pattern,
        RegexpOptions? options = null)
    {
        _source = source;
        _pattern = pattern;
        _options = options?.ToValue();
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RegexpLike)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_pattern)
        .PrependCommaIfNotNull(_options)
        .CloseParenthesis();
}
