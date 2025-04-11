namespace InlineSqlSharp;

public sealed class NumericNull : NumericExpr
{
    public override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);
}
