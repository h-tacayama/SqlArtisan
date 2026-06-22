namespace SqlArtisan.Internal;

/// <summary>
/// The <c>CUBE(...)</c> grouping extension, built with <c>Sql.Cube(...)</c>.
/// Emitted as <c>CUBE(a, b)</c> on PostgreSQL / Oracle / SQL Server; MySQL and
/// SQLite have no CUBE and throw at build time.
/// </summary>
public sealed class CubeGrouping : GroupingElement
{
    private readonly SqlPart[] _columns;

    internal CubeGrouping(SqlPart[] columns)
    {
        if (columns.Length == 0)
        {
            throw new ArgumentException("CUBE requires at least one column.");
        }

        _columns = columns;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendCube(_columns);
}
