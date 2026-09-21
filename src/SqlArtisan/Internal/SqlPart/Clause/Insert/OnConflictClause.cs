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
        ColumnListGuard.ThrowIfDuplicate(
            conflictTarget, "An ON CONFLICT target must not name a column twice.");
        _conflictTarget = conflictTarget;
    }

    // Read by InsertBuilder.Validate: PostgreSQL requires a conflict target for
    // DO UPDATE; SQLite takes the targetless form (ADR 0011).
    internal bool HasTarget => _conflictTarget.Length > 0;

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
