namespace SqlArtisan.Internal;

/// <summary>
/// A <c>BETWEEN start AND end</c> window-frame extent.
/// </summary>
internal sealed class FrameBetween : SqlPart
{
    private readonly FrameBound _start;
    private readonly FrameBound _end;

    internal FrameBetween(FrameBound start, FrameBound end)
    {
        _start = start;
        _end = end;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Between)
        .AppendSpace()
        .Append(_start)
        .EncloseInSpaces(Keywords.And)
        .Append(_end);
}
