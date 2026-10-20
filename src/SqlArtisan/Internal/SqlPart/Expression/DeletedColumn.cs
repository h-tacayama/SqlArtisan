namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>DELETED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the pre-image of a deleted or updated row); renders as <c>DELETED.col</c>.
/// </summary>
public sealed class DeletedColumn : SqlExpression
{
    private readonly DbColumn _column;

    internal DeletedColumn(DbColumn column)
    {
        _column = column;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (!buffer.ReadsPseudoRowTarget(_column))
        {
            throw new ArgumentException(
                $"{Keywords.Deleted} must name a column of the statement's target table.");
        }

        buffer
            .Append($"{Keywords.Deleted}.")
            .Append(_column.Name);
    }
}
