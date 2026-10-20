namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>INSERTED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the post-image of an inserted or updated row); renders as <c>INSERTED.col</c>.
/// </summary>
public sealed class InsertedColumn : SqlExpression
{
    private readonly DbColumn _column;

    internal InsertedColumn(DbColumn column)
    {
        _column = column;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (!buffer.ReadsPseudoRowTarget(_column))
        {
            throw new ArgumentException(
                $"{Keywords.Inserted} must name a column of the statement's target table.");
        }

        buffer
            .Append($"{Keywords.Inserted}.")
            .Append(_column.Name);
    }
}
