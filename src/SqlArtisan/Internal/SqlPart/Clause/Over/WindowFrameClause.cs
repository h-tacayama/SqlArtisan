namespace SqlArtisan.Internal;

/// <summary>
/// A window specification with a frame: an <c>ORDER BY</c> (optionally preceded
/// by <c>PARTITION BY</c>) followed by a <c>ROWS</c> / <c>RANGE</c> frame.
/// </summary>
public sealed class WindowFrameClause : SqlPart
{
    private readonly SqlPart _windowOrdering;
    private readonly WindowFrame _frame;

    internal WindowFrameClause(SqlPart windowOrdering, WindowFrame frame)
    {
        _windowOrdering = windowOrdering;
        _frame = frame;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_windowOrdering)
        .Append(_frame);
}
