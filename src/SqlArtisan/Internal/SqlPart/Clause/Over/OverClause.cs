namespace SqlArtisan.Internal;

internal sealed class OverClause : SqlPart
{
    private readonly SqlPart? _content;

    private OverClause(SqlPart? content = null)
    {
        _content = content;
    }

    internal static OverClause Of() => new();

    // Each overload's parameter matches the public Over(...) parameter it
    // guards, so ParamName never surfaces an internal name.
    internal static OverClause Of(PartitionByClause partitionByClause) =>
        new(NullGuard.ThrowIfNull(partitionByClause, nameof(partitionByClause)));

    internal static OverClause Of(OrderByClause orderByClause) =>
        new(NullGuard.ThrowIfNull(orderByClause, nameof(orderByClause)));

    internal static OverClause Of(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(NullGuard.ThrowIfNull(partitionByAndOrderBy, nameof(partitionByAndOrderBy)));

    internal static OverClause Of(WindowFrameClause windowFrameClause) =>
        new(NullGuard.ThrowIfNull(windowFrameClause, nameof(windowFrameClause)));

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Over)
            .AppendSpace()
            .OpenParenthesis();

        _content?.Format(buffer);

        buffer.CloseParenthesis();
    }
}
