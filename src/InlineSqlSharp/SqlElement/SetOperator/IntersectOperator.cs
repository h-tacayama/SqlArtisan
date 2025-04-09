namespace InlineSqlSharp;

public sealed class IntersectOperator(bool all) : ISqlElement
{
    private readonly bool _all = all;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.INTERSECT)
        .PrependSpaceIf(_all, Keywords.ALL);
}
