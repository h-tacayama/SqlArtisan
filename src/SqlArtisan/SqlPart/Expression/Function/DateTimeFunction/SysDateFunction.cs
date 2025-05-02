namespace SqlArtisan;

public sealed class SysDateFunction : SqlExpression
{
    internal SysDateFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysDate);
}
