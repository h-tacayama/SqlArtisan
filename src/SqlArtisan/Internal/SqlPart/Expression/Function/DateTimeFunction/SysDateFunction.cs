namespace SqlArtisan.Internal;

public sealed class SysDateFunction : SqlExpression
{
    internal SysDateFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysDate);
}
