namespace SqlArtisan.Internal;

internal sealed class SystimestampFunction : SqlExpression
{
    internal SystimestampFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Systimestamp);
}
