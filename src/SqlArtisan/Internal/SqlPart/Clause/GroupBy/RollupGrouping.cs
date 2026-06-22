namespace SqlArtisan.Internal;

/// <summary>
/// The <c>ROLLUP(...)</c> grouping extension, built with <c>Sql.Rollup(...)</c>.
/// Emitted as the standard <c>ROLLUP(a, b)</c> on PostgreSQL / Oracle / SQL
/// Server and as the suffix form <c>a, b WITH ROLLUP</c> on MySQL; SQLite has no
/// ROLLUP and throws at build time.
/// </summary>
public sealed class RollupGrouping : GroupingElement
{
    private readonly SqlPart[] _columns;

    internal RollupGrouping(SqlPart[] columns)
    {
        if (columns.Length == 0)
        {
            throw new ArgumentException("ROLLUP requires at least one column.");
        }

        _columns = columns;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendRollup(_columns);
}
