namespace SqlArtisan.Internal;

/// <summary>
/// References a column of the <c>INSERTED</c> pseudo-table inside a SQL Server
/// <c>OUTPUT</c> clause — the post-image of an inserted or updated row. Renders as
/// <c>INSERTED.col</c>.
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
