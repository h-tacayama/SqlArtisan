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
    public static OfClause Of(DbColumn tableIdentifier) => new(tableIdentifier);

    /// <summary>
    /// An <c>ORDER BY</c> list. Each item is a column or expression, optionally with
    /// a direction (<c>.Asc()</c> / <c>.Desc()</c>) and null placement, and is
    /// emitted as <c>ORDER BY a, b DESC</c>. Used as a query clause and inside
    /// ordered aggregates such as <c>GroupConcat</c>, <c>StringAgg</c>, and
    /// <c>WithinGroup</c>.
    /// </summary>
    /// <param name="orderByItems">The columns or expressions to order by.</param>
    /// <returns>An <c>ORDER BY</c> clause.</returns>
    public static OrderByClause OrderBy(
        params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
