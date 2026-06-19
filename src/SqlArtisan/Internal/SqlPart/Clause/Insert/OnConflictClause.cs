namespace SqlArtisan.Internal;

internal sealed class OnConflictClause(DbColumn[] conflictTarget) : SqlPart
{
    private readonly DbColumn[] _conflictTarget = conflictTarget;

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
