namespace SqlArtisan.Internal;

// A SQL Server extension to MERGE's WHEN family.
internal sealed class WhenNotMatchedBySourceClause(SqlCondition? extraCondition) : SqlPart
{
    private readonly SqlCondition? _extraCondition = extraCondition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(
            $"{Keywords.When} {Keywords.Not} {Keywords.Matched} {Keywords.By} {Keywords.Source}");

        if (_extraCondition is not null)
        {
            ConditionGuard.ThrowIfEmpty(
                _extraCondition,
                "A MERGE WHEN NOT MATCHED BY SOURCE AND clause requires a condition.");

            buffer.EncloseInSpaces(Keywords.And).Append(_extraCondition);
        }

        buffer.Append($" {Keywords.Then}");
    }
}
