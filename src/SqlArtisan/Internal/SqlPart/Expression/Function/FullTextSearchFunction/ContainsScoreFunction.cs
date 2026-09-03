namespace SqlArtisan.Internal;

/// <summary>
/// The Oracle Text <c>CONTAINS(column, query)</c> operator: the column's
/// relevance score (0–100) for the query, 0 when there is no match; with a
/// label, <c>SCORE(label)</c> reads it back.
/// </summary>
public sealed class ContainsScoreFunction : SqlExpression
{
    private readonly SqlExpression _column;
    private readonly SqlExpression _query;
    private readonly string? _label;

    internal ContainsScoreFunction(SqlExpression column, SqlExpression query, int? label = null)
    {
        _column = column;
        _query = query;
        _label = label?.ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Contains)
        .OpenParenthesis()
        .Append(_column)
        .PrependComma(_query)
        .PrependCommaIfNotNull(_label)
        .CloseParenthesis();
}
