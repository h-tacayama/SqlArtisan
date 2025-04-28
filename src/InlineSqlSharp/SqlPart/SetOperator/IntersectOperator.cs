namespace InlineSqlSharp;

internal sealed class IntersectOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Intersect)
        .AppendIf(_all, $" {Keywords.All}");
}
