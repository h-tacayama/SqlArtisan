namespace InlineSqlSharp;

public sealed class DualTable : ITableReference
{
    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.DUAL);
}
