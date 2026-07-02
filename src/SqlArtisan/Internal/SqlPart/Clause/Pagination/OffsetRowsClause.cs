namespace SqlArtisan.Internal;

internal sealed class OffsetRowsClause : SqlPart
{
    private readonly BindValue _start;

    internal OffsetRowsClause(int start)
    {
        _start = new BindValue(start);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Offset).AppendSpace()
        .Append(_start)
        .AppendSpace().Append(Keywords.Rows);
}
