namespace SqlArtisan.Internal;

internal sealed class OutputClause : SqlPart
{
    private readonly SqlPart[] _items;

    internal OutputClause(SqlPart[] items)
    {
        _items = items;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Output} ")
        .AppendSelectItems(_items);
}
