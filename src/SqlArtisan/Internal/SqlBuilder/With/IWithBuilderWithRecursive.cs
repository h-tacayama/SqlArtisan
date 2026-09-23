namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WITH RECURSIVE</c> clause: open the main statement that draws on the
/// CTEs — a <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c>.
/// </summary>
/// <remarks>
/// <c>MERGE</c> is absent on purpose: no supported engine takes a recursive <c>WITH</c> before
/// one, and PostgreSQL 16 — the only engine with both the keyword and the statement — refuses
/// the pairing whether or not the CTE body recurses.
/// </remarks>
public interface IWithBuilderWithRecursive :
    IDeleteBuilder,
    IInsertBuilder,
    ISelectBuilder,
    IUpdateBuilder
{
}
