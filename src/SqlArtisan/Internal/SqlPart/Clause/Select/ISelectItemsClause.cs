namespace SqlArtisan.Internal;

// Lets a consumer reach the resolved select items across every SELECT-clause
// variant without unwrapping the concrete type (the ITopSelectClause pattern);
// WithRecursiveClause derives the CTE column list from the anchor's items.
internal interface ISelectItemsClause
{
    SqlPart[] SelectItems { get; }
}
