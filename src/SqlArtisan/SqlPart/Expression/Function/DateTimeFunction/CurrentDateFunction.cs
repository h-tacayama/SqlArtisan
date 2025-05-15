namespace SqlArtisan;

public sealed class CurrentDateFunction : SqlExpression
{
    internal CurrentDateFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.CurrentDate);
}
