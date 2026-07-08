namespace SqlArtisan.Internal;

// Emits `WHEN NOT MATCHED [AND <condition>] THEN`. The INSERT action follows as a
// separate, space-separated part.
internal sealed class WhenNotMatchedClause(SqlCondition? extraCondition) : SqlPart
{
    private readonly SqlCondition? _extraCondition = extraCondition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.When} {Keywords.Not} {Keywords.Matched}");

        if (_extraCondition is not null)
        {
            ConditionGuard.ThrowIfEmpty(
                _extraCondition,
                "A MERGE WHEN NOT MATCHED AND clause requires a condition.");

            buffer.EncloseInSpaces(Keywords.And).Append(_extraCondition);
        }

        buffer.Append($" {Keywords.Then}");
    }
}
