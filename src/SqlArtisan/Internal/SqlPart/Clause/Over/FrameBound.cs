namespace SqlArtisan.Internal;

/// <summary>
/// A single window-frame bound, such as <c>UNBOUNDED PRECEDING</c>,
/// <c>CURRENT ROW</c>, or <c>n PRECEDING</c>.
/// </summary>
public sealed class FrameBound : SqlPart
{
    private readonly string? _offset;
    private readonly string _keyword;

    private FrameBound(string? offset, string keyword, FrameBoundKind kind)
    {
        _offset = offset;
        _keyword = keyword;
        Kind = kind;
    }

    internal FrameBoundKind Kind { get; }

    internal static FrameBound UnboundedPreceding() =>
        new(null, $"{Keywords.Unbounded} {Keywords.Preceding}", FrameBoundKind.UnboundedPreceding);

    internal static FrameBound Preceding(int offset) =>
        new(
            WindowFrameGuard.ValidateOffset(offset, Keywords.Preceding).ToInvariantString(),
            Keywords.Preceding,
            FrameBoundKind.Preceding);

    internal static FrameBound CurrentRow() =>
        new(null, $"{Keywords.Current} {Keywords.Row}", FrameBoundKind.CurrentRow);

    internal static FrameBound Following(int offset) =>
        new(
            WindowFrameGuard.ValidateOffset(offset, Keywords.Following).ToInvariantString(),
            Keywords.Following,
            FrameBoundKind.Following);

    internal static FrameBound UnboundedFollowing() =>
        new(null, $"{Keywords.Unbounded} {Keywords.Following}", FrameBoundKind.UnboundedFollowing);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_offset is not null)
        {
            buffer.Append(_offset).AppendSpace();
        }

        buffer.Append(_keyword);
    }
}
