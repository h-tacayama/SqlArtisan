namespace SqlArtisan.Internal;

/// <summary>
/// The SQL Server full-text <c>CONTAINS(column, searchCondition)</c> predicate,
/// matching rows whose column satisfies the full-text search condition.
/// </summary>
public sealed class ContainsCondition : SqlCondition
{
    private readonly SqlExpression _column;
    private readonly SqlExpression _searchCondition;

    internal ContainsCondition(SqlExpression column, SqlExpression searchCondition)
    {
        _column = column;
        _searchCondition = searchCondition;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Contains)
        .OpenParenthesis()
        .Append(_column)
        .PrependComma(_searchCondition)
        .CloseParenthesis();
}
