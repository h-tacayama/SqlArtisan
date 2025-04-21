namespace InlineSqlSharp;

public sealed class DualTable : AbstractTableReference
{
    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.DUAL);
}
