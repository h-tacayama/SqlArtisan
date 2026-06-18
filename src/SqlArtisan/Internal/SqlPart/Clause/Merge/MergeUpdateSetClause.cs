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
        new(UpsertAssignmentResolver.Resolve(items));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Update} {Keywords.Set} ")
        .AppendCsv(_assignments);
}
