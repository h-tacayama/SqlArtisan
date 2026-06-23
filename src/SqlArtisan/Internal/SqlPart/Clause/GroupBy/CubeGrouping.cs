namespace SqlArtisan.Internal;

/// <summary>
/// The <c>CUBE(...)</c> grouping extension, built with <c>Sql.Cube(...)</c>.
/// Each element is an ordinary column or a <c>Sql.Group(...)</c> composite column.
/// Emitted as <c>CUBE(a, b)</c>. MySQL and SQLite have no CUBE, but Build emits it
/// faithfully (ADR 0003: feasibility is the analyzer's concern, not Build's).
/// </summary>
public sealed class CubeGrouping : GroupingElement
{
    private readonly SqlPart[] _elements;

    internal CubeGrouping(SqlPart[] elements)
    {
        if (elements.Length == 0)
        {
            throw new ArgumentException("CUBE requires at least one element.");
        }

        _elements = elements;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendCube(_elements);
}
