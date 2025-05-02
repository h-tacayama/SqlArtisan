namespace SqlArtisan;

public sealed class SysTimestampFunction : SqlExpression
{
    internal SysTimestampFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysTimestamp);
}
