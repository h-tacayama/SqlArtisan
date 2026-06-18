namespace SqlArtisan.Internal;

// Emits `WHEN MATCHED [AND <condition>] THEN`. The action (UPDATE SET / DELETE)
// follows as a separate, space-separated part.
internal sealed class WhenMatchedClause(SqlCondition? extraCondition) : SqlPart
{
    private readonly SqlCondition? _extraCondition = extraCondition;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.When} {Keywords.Matched}");

        if (_extraCondition is not null)
        {
            buffer.Append($" {Keywords.And} ").Append(_extraCondition);
        }

        buffer.Append($" {Keywords.Then}");
    }
}
