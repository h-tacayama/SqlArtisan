namespace InlineSqlSharp;

public sealed class SysDateFunction : AbstractExpr
{
    internal SysDateFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SysDate);
}
