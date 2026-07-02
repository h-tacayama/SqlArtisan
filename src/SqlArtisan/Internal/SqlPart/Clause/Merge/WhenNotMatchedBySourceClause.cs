namespace SqlArtisan.Internal;

// Emits `WHEN NOT MATCHED BY SOURCE [AND <condition>] THEN`. This is a SQL Server
// extension; the action (UPDATE SET / DELETE) follows as a separate part.
internal sealed class WhenNotMatchedBySourceClause(SqlCondition? extraCondition) : SqlPart
{
    private readonly SqlCondition? _extraCondition = extraCondition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(
            $"{Keywords.When} {Keywords.Not} {Keywords.Matched} {Keywords.By} {Keywords.Source}");

        if (_extraCondition is not null)
        {
            buffer.EncloseInSpaces(Keywords.And).Append(_extraCondition);
        }

        buffer.AppendSpace().Append(Keywords.Then);
    }
}
