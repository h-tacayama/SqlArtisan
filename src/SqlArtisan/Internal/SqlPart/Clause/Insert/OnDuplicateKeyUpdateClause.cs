namespace SqlArtisan.Internal;

internal sealed class OnDuplicateKeyUpdateClause : SqlPart
{
    private readonly EqualCondition[] _assignments;

    private OnDuplicateKeyUpdateClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static OnDuplicateKeyUpdateClause Parse(EqualityCondition[] items) =>
        new(UpsertAssignmentResolver.Resolve(
            items,
            "ON DUPLICATE KEY UPDATE requires at least one assignment."));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.On} {Keywords.Duplicate} {Keywords.Key} {Keywords.Update} ")
        .AppendAssignmentsCsv(_assignments);
}
