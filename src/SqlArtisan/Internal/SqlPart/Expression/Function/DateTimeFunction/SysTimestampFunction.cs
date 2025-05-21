namespace SqlArtisan.Internal;

public sealed class SysTimestampFunction : SqlExpression
{
    internal SysTimestampFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysTimestamp);
}
