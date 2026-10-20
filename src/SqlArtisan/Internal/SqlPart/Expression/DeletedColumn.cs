namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>DELETED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the pre-image of a deleted or updated row); renders as <c>DELETED.col</c>.
/// </summary>
public sealed class DeletedColumn : SqlExpression
{
    private readonly string _columnName;
    private readonly TableReference _owner;

    internal DeletedColumn(DbColumn column)
    {
        _columnName = column.Name;
        _owner = column.Owner;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.ThrowIfNotPseudoRowTarget(_owner, Keywords.Deleted);
        buffer
            .Append($"{Keywords.Deleted}.")
            .Append(_columnName);
    }
}
