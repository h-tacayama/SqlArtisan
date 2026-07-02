namespace SqlArtisan.Internal;

internal sealed class LimitClause : SqlPart
{
    private readonly BindValue _count;

    internal LimitClause(int count)
    {
        _count = new BindValue(count);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Limit).AppendSpace()
        .Append(_count);
}
