namespace SqlArtisan.Internal;

/// <summary>
/// The row-locking clause that can terminate a query: <c>FOR UPDATE</c>, optionally restricted by
/// <c>OF</c> and with a lock-wait behavior.
/// </summary>
public interface IForUpdate
{
    /// <summary>
    /// Appends <c>FOR UPDATE</c>, locking the selected rows.
    /// </summary>
    /// <param name="lockBehavior">The lock-wait behavior to append — <see cref="Sql.Nowait"/>, <see cref="Sql.SkipLocked"/>, or <c>Sql.Wait(n)</c> (e.g. <c>FOR UPDATE NOWAIT</c>); omit for a plain blocking lock.</param>
    /// <returns>The builder positioned after <c>FOR UPDATE</c>, ready to build or embed as a subquery.</returns>
    /// <remarks>MySQL, Oracle, and PostgreSQL syntax.</remarks>
    ISelectBuilderForUpdate ForUpdate(LockBehaviorBase? lockBehavior = null);

    /// <inheritdoc cref="ForUpdate(LockBehaviorBase?)"/>
    /// <param name="ofClause">What to lock, from <see cref="Sql.Of(DbColumn)"/> (<c>FOR UPDATE OF code</c>) or <see cref="Sql.Of(DbTableBase, DbTableBase[])"/> (<c>FOR UPDATE OF "u", "o"</c>).</param>
    /// <param name="lockBehavior">The lock-wait behavior to append — <see cref="Sql.Nowait"/>, <see cref="Sql.SkipLocked"/>, or <c>Sql.Wait(n)</c>; omit for a plain blocking lock.</param>
    ISelectBuilderForUpdate ForUpdate(OfClause ofClause, LockBehaviorBase? lockBehavior = null);
}
