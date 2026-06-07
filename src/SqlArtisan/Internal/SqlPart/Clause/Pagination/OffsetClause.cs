namespace SqlArtisan.Internal;

internal sealed class OffsetClause : SqlPart
{
    private readonly BindValue _start;

    internal OffsetClause(int start)
    {
        _start = new BindValue(start);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Offset} ")
        .Append(_start);
}
