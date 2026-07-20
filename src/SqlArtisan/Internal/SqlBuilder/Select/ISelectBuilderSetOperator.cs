namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after a set operator (<c>UNION</c>, <c>INTERSECT</c>, <c>EXCEPT</c>, <c>MINUS</c>): supply the next <c>SELECT</c> of the compound query. Not buildable until that <c>SELECT</c> completes.
/// </summary>
public interface ISelectBuilderSetOperator : ISelectBuilder
{
}
