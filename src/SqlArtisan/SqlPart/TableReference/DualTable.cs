namespace SqlArtisan;

public sealed class DualTable : TableReference
{
    internal DualTable() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Dual);
}
