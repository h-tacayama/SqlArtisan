namespace SqlArtisan.Internal;

/// <summary>
/// The row-locking clause that can terminate a query: <c>FOR UPDATE</c>, optionally restricted to named columns and with a lock-wait behavior.
/// </summary>
public interface IForUpdate
{
    /// <summary>
    /// Appends <c>FOR UPDATE</c>, locking the selected rows.
    /// </summary>
    /// <param name="lockBehavior">The lock-wait behavior to append — <see cref="Sql.Nowait"/>, <see cref="Sql.SkipLocked"/>, or <c>Sql.Wait(n)</c> (e.g. <c>FOR UPDATE NOWAIT</c>); omit for a plain blocking lock.</param>
    /// <returns>The terminal builder, ready to build.</returns>
    ISqlBuilder ForUpdate(LockBehaviorBase? lockBehavior = null);

    /// <inheritdoc cref="ForUpdate(LockBehaviorBase?)"/>
    /// <param name="ofClause">The columns to lock on, emitted as <c>OF col, ...</c> (<c>FOR UPDATE OF code</c>).</param>
    /// <param name="lockBehavior">The lock-wait behavior to append — <see cref="Sql.Nowait"/>, <see cref="Sql.SkipLocked"/>, or <c>Sql.Wait(n)</c>; omit for a plain blocking lock.</param>
    ISqlBuilder ForUpdate(
        OfClause ofClause,
        LockBehaviorBase? lockBehavior = null);
}
