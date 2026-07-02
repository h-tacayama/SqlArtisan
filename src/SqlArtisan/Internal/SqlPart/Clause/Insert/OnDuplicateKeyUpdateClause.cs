namespace SqlArtisan.Internal;

internal sealed class OnDuplicateKeyUpdateClause : SqlPart
{
    private readonly EqualityCondition[] _assignments;

    private OnDuplicateKeyUpdateClause(EqualityCondition[] assignments)
    {
        _assignments = assignments;
    }

    internal static OnDuplicateKeyUpdateClause Parse(EqualityBasedCondition[] items) =>
        new(UpsertAssignmentResolver.Resolve(items));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.On} {Keywords.Duplicate} {Keywords.Key} {Keywords.Update}").AppendSpace()
        .AppendAssignmentsCsv(_assignments);
}
