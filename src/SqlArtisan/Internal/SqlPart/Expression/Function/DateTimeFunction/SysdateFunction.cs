namespace SqlArtisan.Internal;

internal sealed class SysdateFunction : SqlExpression
{
    internal SysdateFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Sysdate);
}
