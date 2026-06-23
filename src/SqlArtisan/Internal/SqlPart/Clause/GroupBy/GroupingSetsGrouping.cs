namespace SqlArtisan.Internal;

/// <summary>
/// The <c>GROUPING SETS(...)</c> grouping extension, built with
/// <c>Sql.GroupingSets(...)</c> from one or more <c>Sql.Group(...)</c> sets.
/// Emitted as <c>GROUPING SETS((a, b), c, ())</c>. MySQL and SQLite have no
/// GROUPING SETS, but Build emits it faithfully (ADR 0003: feasibility is the
/// analyzer's concern, not Build's).
/// </summary>
public sealed class GroupingSetsGrouping : GroupingElement
{
    private readonly GroupingSet[] _sets;

    internal GroupingSetsGrouping(GroupingSet[] sets)
    {
        if (sets.Length == 0)
        {
            throw new ArgumentException(
                "GROUPING SETS requires at least one grouping set.");
        }

        _sets = sets;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendGroupingSets(_sets);
}
