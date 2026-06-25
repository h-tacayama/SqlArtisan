namespace SqlArtisan.Internal;

/// <summary>Marks a builder state whose query can be embedded as a subquery (a derived table, an <c>IN</c>/<c>EXISTS</c> operand, or a scalar subquery).</summary>
public interface ISubquery
{
    internal void Format(SqlBuildingBuffer buffer);
}
