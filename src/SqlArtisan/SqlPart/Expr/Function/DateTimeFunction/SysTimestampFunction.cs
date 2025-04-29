namespace SqlArtisan;

public sealed class SysTimestampFunction : AbstractExpr
{
    internal SysTimestampFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysTimestamp);
}
