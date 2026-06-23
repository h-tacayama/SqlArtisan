namespace SqlArtisan.Internal;

/// <summary>
/// The <c>ROLLUP(...)</c> grouping extension, built with <c>Sql.Rollup(...)</c>.
/// Each element is an ordinary column or a <c>Sql.Group(...)</c> composite column.
/// Emitted as the standard <c>ROLLUP(a, b)</c> on PostgreSQL / Oracle / SQL
/// Server and as the suffix form <c>a, b WITH ROLLUP</c> on MySQL. SQLite has no
/// ROLLUP, but Build emits it faithfully (ADR 0003: feasibility is the analyzer's
/// concern, not Build's).
/// </summary>
public sealed class RollupGrouping : GroupingElement
{
    private readonly SqlPart[] _elements;

    internal RollupGrouping(SqlPart[] elements)
    {
        if (elements.Length == 0)
        {
            throw new ArgumentException("ROLLUP requires at least one element.");
        }

        _elements = elements;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendRollup(_elements);
}
