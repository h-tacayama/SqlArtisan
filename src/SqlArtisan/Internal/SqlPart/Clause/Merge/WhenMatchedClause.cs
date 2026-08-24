namespace SqlArtisan.Internal;

// The WHEN branch keyword only — its action follows as a separate,
// space-separated part, the shape all three WHEN clauses share.
internal sealed class WhenMatchedClause(SqlCondition? extraCondition) : SqlPart
{
    private readonly SqlCondition? _extraCondition = extraCondition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.When} {Keywords.Matched}");

        if (_extraCondition is not null)
        {
            ConditionGuard.ThrowIfEmpty(
                _extraCondition,
                "A MERGE WHEN MATCHED AND clause requires a condition.");

            buffer.EncloseInSpaces(Keywords.And).Append(_extraCondition);
        }

        buffer.Append($" {Keywords.Then}");
    }
}
