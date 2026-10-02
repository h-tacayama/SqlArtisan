namespace SqlArtisan.Internal;

internal sealed class DoUpdateSetClause : SqlPart
{
    private const string Clause = $"{Keywords.Do} {Keywords.Update} {Keywords.Set}";

    private readonly EqualCondition[] _assignments;

    private DoUpdateSetClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static DoUpdateSetClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(assignments, Clause);
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: false, Clause);

        return new(resolved);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Clause} ")
        .AppendAssignmentsCsv(_assignments);
}
