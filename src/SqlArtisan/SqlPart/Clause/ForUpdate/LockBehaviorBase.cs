namespace SqlArtisan;

/// <summary>
/// A <c>FOR UPDATE</c> lock-wait option: <c>NOWAIT</c>, <c>SKIP LOCKED</c>, or Oracle's
/// <c>WAIT n</c>, from <see cref="Sql.Nowait"/>, <see cref="Sql.SkipLocked"/>, or
/// <see cref="Sql.Wait(int)"/>.
/// </summary>
public abstract class LockBehaviorBase : SqlPart
{
    private protected LockBehaviorBase()
    {
    }
}
