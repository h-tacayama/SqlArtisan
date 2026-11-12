namespace SqlArtisan.Internal;

internal sealed class MergeIntoClause : SqlPart
{
    private readonly DbTableBase _target;

    internal MergeIntoClause(DbTableBase target)
    {
        DmlTargetGuard.ThrowIfMergeTargetUnaliased(target);
        _target = target;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Merge} {Keywords.Into} ")
        .Append(_target);
}
