namespace InlineSqlSharp;

internal sealed class IntersectOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.INTERSECT)
        .PrependSpaceIf(_all, Keywords.ALL);
}
