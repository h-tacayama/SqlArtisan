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

    // A non-empty element list is guaranteed by Sql.Rollup, whose required leading
    // element the resolver always carries through; the constructor trusts that.
    internal RollupGrouping(SqlPart[] elements)
    {
        _elements = elements;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendRollup(_elements);
}
