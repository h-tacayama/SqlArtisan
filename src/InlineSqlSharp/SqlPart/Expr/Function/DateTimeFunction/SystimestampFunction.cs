namespace InlineSqlSharp;

public sealed class SystimestampFunction : AbstractExpr
{
    internal SystimestampFunction() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.SYSTIMESTAMP);
}
