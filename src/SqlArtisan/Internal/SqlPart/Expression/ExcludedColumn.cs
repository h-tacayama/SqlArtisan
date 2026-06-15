namespace SqlArtisan.Internal;

/// <summary>
/// References a column of the row proposed for insertion inside an UPSERT
/// update clause. Renders as <c>EXCLUDED.col</c> (PostgreSQL),
/// <c>excluded.col</c> (SQLite), or <c>new.col</c> (MySQL), resolved by the
/// dialect at build time.
/// </summary>
public sealed class ExcludedColumn : SqlExpression
{
    private readonly string _columnName;

    internal ExcludedColumn(DbColumn column)
    {
        _columnName = column.Name;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendExcludedReference()
        .Append('.')
        .Append(_columnName);
}
