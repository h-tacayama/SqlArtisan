namespace SqlArtisan.Internal;

internal sealed class OnDuplicateKeyUpdateClause : SqlPart
{
    private readonly EqualCondition[] _assignments;

    private OnDuplicateKeyUpdateClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static OnDuplicateKeyUpdateClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(
            assignments, "ON DUPLICATE KEY UPDATE requires at least one assignment.");
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: false);

        return new(resolved);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.On} {Keywords.Duplicate} {Keywords.Key} {Keywords.Update} ")
        .AppendAssignmentsCsv(_assignments);
}
