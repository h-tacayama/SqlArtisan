namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WITH RECURSIVE</c> clause: open the main statement that draws on the
/// CTEs — a <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c>.
/// </summary>
public interface IWithBuilderWithRecursive :
    IDeleteBuilder,
    IInsertBuilder,
    ISelectBuilder,
    IUpdateBuilder
{
    // No MERGE: PostgreSQL 16, the only engine with both RECURSIVE and MERGE, refuses the
    // pairing whether or not the CTE body recurses.
}
