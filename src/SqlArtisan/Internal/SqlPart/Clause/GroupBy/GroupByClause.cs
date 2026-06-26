namespace SqlArtisan.Internal;

internal sealed class GroupByClause : SqlPart
{
    private readonly SqlPart[] _groupByItems;

    private GroupByClause(SqlPart[] groupByItems)
    {
        if (groupByItems.Length == 0)
        {
            throw new ArgumentException(
                "GROUP BY requires at least one item.");
        }

        _groupByItems = groupByItems;
    }

    internal static GroupByClause Parse(object[] groupByItems) =>
        new(GroupByItemResolver.Resolve(groupByItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Group} {Keywords.By} ")
        .AppendCsv(_groupByItems);
}
