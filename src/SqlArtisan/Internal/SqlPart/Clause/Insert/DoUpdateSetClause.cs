namespace SqlArtisan.Internal;

internal sealed class DoUpdateSetClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;

    private DoUpdateSetClause(EqualityCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static DoUpdateSetClause Parse(EqualityBasedCondition[] items) =>
        new(UpsertAssignmentResolver.Resolve(items));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Do} {Keywords.Update} {Keywords.Set} ")
        .AppendCsv(_assignments);
}
