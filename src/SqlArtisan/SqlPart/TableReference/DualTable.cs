namespace InlineSqlSharp;

public sealed class DualTable : AbstractTableReference
{
    internal DualTable() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Dual);
}
