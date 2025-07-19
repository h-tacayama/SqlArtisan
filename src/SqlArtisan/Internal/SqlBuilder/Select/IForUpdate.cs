namespace SqlArtisan.Internal;

public interface IForUpdate
{
    ISqlBuilder ForUpdate(LockBehaviorBase? lockBehavior = null);

    ISqlBuilder ForUpdate(
        OfClause ofClause,
        LockBehaviorBase? lockBehavior = null);
}
