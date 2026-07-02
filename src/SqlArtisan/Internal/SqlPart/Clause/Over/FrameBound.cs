namespace SqlArtisan.Internal;

/// <summary>
/// A single window-frame bound, such as <c>UNBOUNDED PRECEDING</c>,
/// <c>CURRENT ROW</c>, or <c>n PRECEDING</c>.
/// </summary>
public sealed class FrameBound : SqlPart
{
    private readonly string? _offset;
    private readonly string _keyword;

    private FrameBound(string? offset, string keyword)
    {
        _offset = offset;
        _keyword = keyword;
    }

    internal static FrameBound UnboundedPreceding() =>
        new(null, $"{Keywords.Unbounded} {Keywords.Preceding}");

    internal static FrameBound Preceding(int offset) =>
        new(offset.ToInvariantString(), Keywords.Preceding);

    internal static FrameBound CurrentRow() =>
        new(null, $"{Keywords.Current} {Keywords.Row}");

    internal static FrameBound Following(int offset) =>
        new(offset.ToInvariantString(), Keywords.Following);

    internal static FrameBound UnboundedFollowing() =>
        new(null, $"{Keywords.Unbounded} {Keywords.Following}");

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_offset is not null)
        {
            buffer
                .Append(_offset)
                .AppendSpace();
        }

        buffer.Append(_keyword);
    }
}
