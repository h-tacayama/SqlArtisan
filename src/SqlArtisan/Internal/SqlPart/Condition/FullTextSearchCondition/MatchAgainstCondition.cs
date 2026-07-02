namespace SqlArtisan.Internal;

/// <summary>
/// The MySQL full-text <c>MATCH (columns) AGAINST (searchExpr [modifier])</c>
/// predicate, matching rows whose relevance score is non-zero.
/// </summary>
public sealed class MatchAgainstCondition : SqlCondition
{
    private readonly SqlExpression[] _columns;
    private readonly SqlExpression _searchExpr;
    private readonly string? _modifier;

    internal MatchAgainstCondition(
        SqlExpression[] columns,
        SqlExpression searchExpr,
        SearchModifier? modifier)
    {
        _columns = columns;
        _searchExpr = searchExpr;
        _modifier = modifier?.ToKeyword();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Match)
        .AppendSpace()
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Against)
        .OpenParenthesis()
        .Append(_searchExpr)
        .PrependSpaceIfNotNull(_modifier)
        .CloseParenthesis();
}
