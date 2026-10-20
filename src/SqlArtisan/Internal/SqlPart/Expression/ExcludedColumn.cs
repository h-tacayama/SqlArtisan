namespace SqlArtisan.Internal;

/// <summary>
/// References a column of the row proposed for insertion inside an UPSERT
/// update clause. Renders as <c>EXCLUDED.col</c> (PostgreSQL),
/// <c>excluded.col</c> (SQLite), or <c>new.col</c> (MySQL), resolved by the
/// dialect at build time.
/// </summary>
internal sealed class ExcludedColumn : SqlExpression
{
    private readonly DbColumn _column;

    internal ExcludedColumn(DbColumn column)
    {
        _column = column;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (!buffer.ReadsPseudoRowTarget(_column))
        {
            throw new ArgumentException(
                $"{buffer.ExcludedName} must name a column of the INSERT target table; read "
                    + "another table's value through a subquery.");
        }

        buffer
            .AppendExcludedName()
            .Append('.')
            .Append(_column.Name);
    }
}
