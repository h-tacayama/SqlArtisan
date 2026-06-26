namespace SqlArtisan.Internal;

/// <summary>
/// The <c>CUBE(...)</c> grouping extension, built with <c>Sql.Cube(...)</c>.
/// Each element is an ordinary column or a <c>Sql.Group(...)</c> composite column.
/// Emitted as <c>CUBE(a, b)</c>. MySQL and SQLite have no CUBE; Build still emits
/// it faithfully, leaving the unsupported statement for the database to reject.
/// </summary>
public sealed class CubeGrouping : GroupingElement
{
    private readonly SqlPart[] _elements;

    // A non-empty element list is guaranteed by Sql.Cube, whose required leading
    // element the resolver always carries through; the constructor trusts that.
    internal CubeGrouping(SqlPart[] elements)
    {
        _elements = elements;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendCube(_elements);
}
