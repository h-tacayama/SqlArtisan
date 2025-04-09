namespace InlineSqlSharp;

public sealed class SysdateFunction : DateTimeExpr
{
    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SYSDATE);
}
