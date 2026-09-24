using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>OF column</c> target for a <c>FOR UPDATE</c> clause, naming the table
    /// (via one of its columns) whose rows are locked.
    /// </summary>
    /// <param name="tableIdentifier">A column of the table to lock.</param>
    /// <returns>An <c>OF</c> clause for <c>FOR UPDATE OF ...</c>.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static OfClause Of(DbColumn tableIdentifier) => new(tableIdentifier);

    /// <summary>
    /// The <c>OF table, ...</c> target for a <c>FOR UPDATE</c> clause, naming the tables whose
    /// rows are locked; the other tables in the query stay unlocked.
    /// </summary>
    /// <param name="table">A table in the query's <c>FROM</c>; renders as its alias when it has one.</param>
    /// <param name="moreTables">Further tables to lock.</param>
    /// <returns>An <c>OF</c> clause for <c>FOR UPDATE OF ...</c>.</returns>
    /// <remarks>
    /// MySQL and PostgreSQL syntax. PostgreSQL rejects a plain <c>FOR UPDATE</c> on an outer
    /// join; naming the preserved side here locks it.
    /// </remarks>
    public static OfClause Of(DbTableBase table, params DbTableBase[] moreTables)
    {
        NullGuard.ThrowIfNull(table, nameof(table));
        CollectionGuard.ThrowIfNullElement(
            moreTables, nameof(moreTables), "A FOR UPDATE OF list must not contain a null table.");
        return new([table, .. moreTables]);
    }

    /// <summary>
    /// An <c>ORDER BY</c> list. Each item is a column or expression, optionally with
    /// a direction (<c>.Asc</c> / <c>.Desc</c>) and null placement, and is
    /// emitted as <c>ORDER BY a, b DESC</c>. Used as a query clause and inside the
    /// ordered aggregates.
    /// </summary>
    /// <param name="orderByItems">The columns or expressions to order by.</param>
    /// <returns>An <c>ORDER BY</c> clause.</returns>
    public static OrderByClause OrderBy(params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
