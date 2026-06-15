namespace SqlArtisan.Internal;

internal sealed class DoUpdateClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;

    private DoUpdateClause(EqualityCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static DoUpdateClause Parse(EqualityBasedCondition[] items) =>
        new(UpsertAssignmentResolver.Resolve(items));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Do} {Keywords.Update} {Keywords.Set} ")
        .AppendCsv(_assignments);
}
