namespace InlineSqlSharp;

public sealed class Null : AbstractExpr
{
    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);
}
