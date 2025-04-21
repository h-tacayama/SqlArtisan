namespace InlineSqlSharp;

public sealed class Null : AbstractExpr
{
    internal Null() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);
}
