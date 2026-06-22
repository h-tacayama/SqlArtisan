namespace SqlArtisan.Internal;

/// <summary>
/// A single grouping set inside <c>GROUPING SETS(...)</c>, built with
/// <c>Sql.Group(...)</c>. Rendered as a parenthesized column list (<c>(a, b)</c>)
/// or, when no columns are given, the empty set <c>()</c> that produces the grand
/// total.
/// </summary>
public sealed class GroupingSet : SqlPart
{
    private readonly SqlPart[] _columns;

    internal GroupingSet(SqlPart[] columns)
    {
        _columns = columns;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .OpenParenthesis()
        .AppendCsv(_columns)
        .CloseParenthesis();
}
