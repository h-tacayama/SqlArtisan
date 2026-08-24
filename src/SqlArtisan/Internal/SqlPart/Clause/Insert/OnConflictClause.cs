namespace SqlArtisan.Internal;

internal sealed class OnConflictClause : SqlPart
{
    private readonly DbColumn[] _conflictTarget;

    internal OnConflictClause(DbColumn[] conflictTarget)
    {
        CollectionGuard.ThrowIfNullElement(
            conflictTarget,
            nameof(conflictTarget),
            "An ON CONFLICT target must not contain a null column.");
        _conflictTarget = conflictTarget;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.On} {Keywords.Conflict}");

        if (_conflictTarget.Length > 0)
        {
            buffer.AppendSpace()
                .OpenParenthesis()
                .AppendUnqualifiedColumnsCsv(_conflictTarget)
                .CloseParenthesis();
        }
    }
}
