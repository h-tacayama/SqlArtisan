namespace SqlArtisan.Internal;

internal sealed class OnDuplicateKeyUpdateClause : SqlPart
{
    private const string Clause =
        $"{Keywords.On} {Keywords.Duplicate} {Keywords.Key} {Keywords.Update}";

    private readonly EqualCondition[] _assignments;

    private OnDuplicateKeyUpdateClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static OnDuplicateKeyUpdateClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(assignments, Clause);
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: false, Clause);

        return new(resolved);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Clause} ")
        .AppendAssignmentsCsv(_assignments);
}
