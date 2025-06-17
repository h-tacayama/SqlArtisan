namespace SqlArtisan.Internal;

public sealed class SystimestampFunction : SqlExpression
{
    internal SystimestampFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Systimestamp);
}
