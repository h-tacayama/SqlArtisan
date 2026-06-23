namespace SqlArtisan.Internal;

/// <summary>
/// The <c>ROLLUP(...)</c> grouping extension, built with <c>Sql.Rollup(...)</c>.
/// Each element is an ordinary column or a <c>Sql.Group(...)</c> composite column.
/// Emitted as the standard function form <c>ROLLUP(a, b)</c> on every dialect.
/// MySQL's <c>WITH ROLLUP</c> suffix is a separate construct
/// (<c>.GroupBy(...).WithRollup()</c>); an unsupported target is emitted as written
/// for the database to reject.
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
