namespace SqlArtisan.Internal;

internal sealed class MergeIntoClause(DbTableBase target) : SqlPart
{
    private readonly DbTableBase _target = target;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Merge} {Keywords.Into}").AppendSpace()
        .Append(_target);
}
