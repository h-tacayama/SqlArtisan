namespace SqlArtisan.Internal;

internal sealed class ForUpdateClause : SqlPart
{
    private readonly OfClause? _ofClause;
    private readonly LockBehaviorBase? _lockBehavior;

    internal ForUpdateClause(LockBehaviorBase? lockBehavior = null)
    {
        _ofClause = null;
        _lockBehavior = lockBehavior;
    }

    internal ForUpdateClause(
        OfClause ofClause,
        LockBehaviorBase? lockBehavior = null)
    {
        _ofClause = ofClause;
        _lockBehavior = lockBehavior;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.For} {Keywords.Update}")
        .PrependSpaceIfNotNull(_ofClause)
        .PrependSpaceIfNotNull(_lockBehavior);
}
