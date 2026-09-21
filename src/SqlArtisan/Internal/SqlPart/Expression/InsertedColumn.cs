namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>INSERTED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the post-image of an inserted or updated row); renders as <c>INSERTED.col</c>.
/// </summary>
public sealed class InsertedColumn : SqlExpression
{
    private readonly string _columnName;

    internal InsertedColumn(DbColumn column)
    {
        _columnName = column.Name;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Inserted}.")
        .Append(_columnName);
}
