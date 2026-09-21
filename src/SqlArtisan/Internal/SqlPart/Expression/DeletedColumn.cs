namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>DELETED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the pre-image of a deleted or updated row); renders as <c>DELETED.col</c>.
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
