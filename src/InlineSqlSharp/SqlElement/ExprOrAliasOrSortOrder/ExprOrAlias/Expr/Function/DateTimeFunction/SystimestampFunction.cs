namespace InlineSqlSharp;

public sealed class SystimestampFunction : DateTimeExpr
{
    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SYSTIMESTAMP);
}
