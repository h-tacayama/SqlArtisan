namespace InlineSqlSharp;

public sealed class SystimestampFunction : AbstractExpr
{
    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SYSTIMESTAMP);
}
