namespace SqlArtisan.Internal;

/// <summary>
/// The builder state after a set operator (<c>UNION</c>, <c>INTERSECT</c>, <c>EXCEPT</c>, <c>MINUS</c>): supply the next <c>SELECT</c> of the compound query.
/// </summary>
public interface ISelectBuilderSetOperator : ISqlBuilder, ISelectBuilder
{
}
