namespace InlineSqlSharp;

public sealed class UnionOperator(bool all) : ISqlElement
{
    private readonly bool _all = all;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.UNION)
        .PrependSpaceIf(_all, Keywords.ALL);
}
