namespace SqlArtisan.Internal;

// The `UPDATE SET col = val, ...` action of a MERGE WHEN clause. Unlike the
// standalone UPDATE statement's SET clause, MERGE leads with the UPDATE keyword.
internal sealed class MergeUpdateSetClause : SqlPart
{
    private const string Clause = $"{Keywords.Update} {Keywords.Set}";

    private readonly EqualCondition[] _assignments;

    private MergeUpdateSetClause(EqualCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static MergeUpdateSetClause Parse(EqualityCondition[] assignments)
    {
        EqualCondition[] resolved = AssignmentResolver.Resolve(assignments, Clause);
        AssignmentResolver.ThrowIfDuplicateTarget(resolved, qualified: false, Clause);

        return new(resolved);
    }

    // MERGE's SET target is a target-table column by grammar, so PostgreSQL
    // rejects any qualification on it — unlike the SQL Server / MySQL joined
    // UPDATE, which qualifies its SET target (UpdateSetClause).
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Clause} ")
        .AppendAssignmentsCsv(_assignments);
}
