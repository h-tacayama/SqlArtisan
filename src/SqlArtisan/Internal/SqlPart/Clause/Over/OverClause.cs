namespace SqlArtisan.Internal;

internal sealed class OverClause : SqlPart
{
    private readonly SqlPart? _content;

    private OverClause(SqlPart? content = null)
    {
        _content = content;
    }

    internal static OverClause Of() => new();

    internal static OverClause Of(PartitionByClause content) =>
        new(NullGuard.ThrowIfNull(content, nameof(content)));

    internal static OverClause Of(OrderByClause content) =>
        new(NullGuard.ThrowIfNull(content, nameof(content)));

    internal static OverClause Of(PartitionByAndOrderBy content) =>
        new(NullGuard.ThrowIfNull(content, nameof(content)));

    internal static OverClause Of(WindowFrameClause content) =>
        new(NullGuard.ThrowIfNull(content, nameof(content)));

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Over)
            .AppendSpace()
            .OpenParenthesis();

        _content?.Format(buffer);

        buffer.CloseParenthesis();
    }
}
