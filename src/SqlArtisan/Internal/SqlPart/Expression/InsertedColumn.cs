namespace SqlArtisan.Internal;

/// <summary>
/// A column of SQL Server's <c>INSERTED</c> pseudo-table in an <c>OUTPUT</c> clause
/// (the post-image of an inserted or updated row); renders as <c>INSERTED.col</c>.
/// </summary>
public sealed class InsertedColumn : SqlExpression
{
    private readonly string _columnName;
    private readonly TableReference _owner;

    internal InsertedColumn(DbColumn column)
    {
        _columnName = column.Name;
        _owner = column.Owner;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.ThrowIfNotPseudoRowTarget(_owner, Keywords.Inserted);
        buffer
            .Append($"{Keywords.Inserted}.")
            .Append(_columnName);
    }
}
