namespace SqlArtisan.Internal;

/// <summary>
/// References a column of the <c>DELETED</c> pseudo-table inside a SQL Server
/// <c>OUTPUT</c> clause — the pre-image of a deleted or updated row. Renders as
/// <c>DELETED.col</c>.
/// </summary>
public sealed class DeletedColumn : SqlExpression
{
    private readonly string _columnName;

    internal DeletedColumn(DbColumn column)
    {
        _columnName = column.Name;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Deleted}.")
        .Append(_columnName);
}
