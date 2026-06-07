namespace SqlArtisan.Internal;

internal sealed class FetchClause : SqlPart
{
    private readonly BindValue _count;
    private readonly bool _first;

    internal FetchClause(int count, bool first)
    {
        _count = new BindValue(count);
        _first = first;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Fetch} {(_first ? Keywords.First : Keywords.Next)} ")
        .Append(_count)
        .Append($" {Keywords.Rows} {Keywords.Only}");
}
