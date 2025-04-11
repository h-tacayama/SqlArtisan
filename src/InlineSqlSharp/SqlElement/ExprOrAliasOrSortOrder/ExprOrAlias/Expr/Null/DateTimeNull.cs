namespace InlineSqlSharp;

public sealed class DateTimeNull : DateTimeExpr
{
    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);
}
