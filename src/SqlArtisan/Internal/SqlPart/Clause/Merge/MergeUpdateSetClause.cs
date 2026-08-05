namespace SqlArtisan.Internal;

// The `UPDATE SET col = val, ...` action of a MERGE WHEN clause. Unlike the
// standalone UPDATE statement's SET clause, MERGE leads with the UPDATE keyword.
internal sealed class MergeUpdateSetClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;

    private MergeUpdateSetClause(EqualityCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static MergeUpdateSetClause Parse(EqualityBasedCondition[] items) =>
        new(UpsertAssignmentResolver.Resolve(
            items,
            "UPDATE SET requires at least one assignment."));

    // MERGE's SET target is a target-table column by grammar, so PostgreSQL
    // rejects any qualification on it — unlike the SQL Server / MySQL joined
    // UPDATE (UpdateSetClause), the one SET position that requires the qualifier.
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Update} {Keywords.Set} ")
        .AppendAssignmentsCsv(_assignments);
}
