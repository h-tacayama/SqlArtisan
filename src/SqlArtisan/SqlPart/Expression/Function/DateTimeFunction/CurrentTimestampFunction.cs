namespace SqlArtisan;

public sealed class CurrentTimestampFunction : SqlExpression
{
    internal CurrentTimestampFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.CurrentTimestamp);
}
