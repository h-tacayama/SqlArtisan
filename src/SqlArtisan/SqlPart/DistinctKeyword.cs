namespace InlineSqlSharp;

public sealed class DistinctKeyword : AbstractSqlPart
{
    internal DistinctKeyword() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Distinct);
}
