using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A grouping element built with <c>Sql.Group(...)</c> for <c>GROUPING SETS(...)</c>,
/// <c>ROLLUP(...)</c> or <c>CUBE(...)</c>: <c>(a, b)</c>, or the bare column for one. With
/// no columns it is the empty set <c>()</c>, a grand total only in <c>GROUPING SETS(...)</c>.
/// </summary>
public sealed class GroupingSet : SqlPart
{
    private readonly SqlPart[] _columns;

    internal GroupingSet(SqlPart[] columns)
    {
        _columns = columns;
    }

    // `(a)` and `a` are equivalent groupings, so a single column drops the
    // redundant parentheses.
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
