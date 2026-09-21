namespace SqlArtisan.Internal;

internal sealed class DoUpdateSetClause : SqlPart
{
    private readonly EqualCondition[] _assignments;

    private DoUpdateSetClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static DoUpdateSetClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(
            assignments, "DO UPDATE SET requires at least one assignment.");
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: false);

        return new(resolved);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Do} {Keywords.Update} {Keywords.Set} ")
        .AppendAssignmentsCsv(_assignments);
}
