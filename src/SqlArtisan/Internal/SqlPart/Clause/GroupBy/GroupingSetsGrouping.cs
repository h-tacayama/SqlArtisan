namespace SqlArtisan.Internal;

/// <summary>
/// The <c>GROUPING SETS(...)</c> grouping extension, built with
/// <c>Sql.GroupingSets(...)</c> from one or more <c>Sql.Group(...)</c> sets.
/// Emitted as <c>GROUPING SETS((a, b), c, ())</c>. MySQL and SQLite have no
/// GROUPING SETS; Build still emits it faithfully, leaving the unsupported
/// statement for the database to reject.
/// </summary>
public sealed class GroupingSetsGrouping : GroupingElement
{
    private readonly GroupingSet[] _sets;

    // The leading set is taken separately so the factory can pass its `params`
    // array straight through: a null array (the C# binding for e.g.
    // GroupingSets(set, null)) throws a clear ArgumentNullException instead of
    // failing with an NRE when spread into a collection expression. The required
    // leading set also guarantees at least one grouping set.
    internal GroupingSetsGrouping(GroupingSet set, params GroupingSet[] sets)
    {
        if (sets is null)
        {
            throw new ArgumentNullException(
                nameof(sets), ExpressionResolver.NullValueMessage);
        }

        _sets = new GroupingSet[sets.Length + 1];
        _sets[0] = set;
        Array.Copy(sets, 0, _sets, 1, sets.Length);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendGroupingSets(_sets);
}
