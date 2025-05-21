namespace SqlArtisan.Internal;

public sealed class CurrentTimeFunction : SqlExpression
{
    internal CurrentTimeFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.CurrentTime);
}
