namespace SqlArtisan.Internal;

/// <summary>
/// A single window-frame bound, such as <c>UNBOUNDED PRECEDING</c>,
/// <c>CURRENT ROW</c>, or <c>n PRECEDING</c>.
/// </summary>
public sealed class FrameBound : SqlPart
{
    private readonly string _text;

    private FrameBound(string text)
    {
        _text = text;
    }

    internal static FrameBound UnboundedPreceding() =>
        new($"{Keywords.Unbounded} {Keywords.Preceding}");

    internal static FrameBound Preceding(int offset) =>
        new($"{offset.ToInvariantString()} {Keywords.Preceding}");

    internal static FrameBound CurrentRow() =>
        new($"{Keywords.Current} {Keywords.Row}");

    internal static FrameBound Following(int offset) =>
        new($"{offset.ToInvariantString()} {Keywords.Following}");

    internal static FrameBound UnboundedFollowing() =>
        new($"{Keywords.Unbounded} {Keywords.Following}");

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_text);
}
