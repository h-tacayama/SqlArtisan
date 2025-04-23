namespace InlineSqlSharp;

public sealed class Distinct : AbstractSqlPart
{
    internal Distinct() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.DISTINCT);
}
