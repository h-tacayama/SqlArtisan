namespace SqlArtisan.Internal;

/// <summary>
/// A parenthesized grouping element built with <c>Sql.Group(...)</c>: a grouping
/// set inside <c>GROUPING SETS(...)</c>, or a composite column inside
/// <c>ROLLUP(...)</c> / <c>CUBE(...)</c>. Rendered as a parenthesized list
/// (<c>(a, b)</c>) for two or more columns, the bare column for a single column,
/// or the empty set <c>()</c> for the grand total when no columns are given.
/// </summary>
public sealed class GroupingSet : SqlPart
{
    private readonly SqlPart[] _columns;

    internal GroupingSet(SqlPart[] columns)
    {
        _columns = columns;
    }

    // True when the set renders with parentheses — an empty set `()` or a
    // multi-column list `(a, b)`. A single column renders bare, so it is not
    // composite. MySQL's `WITH ROLLUP` suffix form cannot express a composite
    // grouping element, so AppendRollup rejects one on that dialect.
    internal bool IsComposite => _columns.Length != 1;

    // A single column renders bare: `(a)` and `a` are equivalent groupings, so the
    // redundant parentheses are dropped. Two or more columns render as a
    // parenthesized list, and an empty set renders as `()`.
    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_columns.Length == 1)
        {
            _columns[0].Format(buffer);
            return;
        }

        buffer
            .OpenParenthesis()
            .AppendCsv(_columns)
            .CloseParenthesis();
    }
}
