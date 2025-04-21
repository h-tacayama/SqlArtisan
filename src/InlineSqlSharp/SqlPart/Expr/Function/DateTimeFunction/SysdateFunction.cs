namespace InlineSqlSharp;

public sealed class SysdateFunction : AbstractExpr
{
    internal SysdateFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SYSDATE);
}
