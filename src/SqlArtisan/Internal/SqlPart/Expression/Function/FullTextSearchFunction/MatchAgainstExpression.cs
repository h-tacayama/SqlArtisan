namespace SqlArtisan.Internal;

/// <summary>
/// The MySQL full-text <c>MATCH (columns) AGAINST (searchExpr [modifier])</c>
/// construct used as a value: the relevance score of the row, for a select list
/// or <c>ORDER BY</c>.
/// </summary>
public sealed class MatchAgainstExpression : SqlExpression
{
    private readonly SqlExpression[] _columns;
    private readonly SqlExpression _searchExpr;
    private readonly string? _modifier;

    internal MatchAgainstExpression(
        SqlExpression[] columns,
        SqlExpression searchExpr,
        SearchModifier? modifier)
    {
        _columns = columns;
        _searchExpr = searchExpr;
        _modifier = modifier?.ToKeyword();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Match} ")
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Against)
        .OpenParenthesis()
        .Append(_searchExpr)
        .PrependSpaceIfNotNull(_modifier)
        .CloseParenthesis();
}
