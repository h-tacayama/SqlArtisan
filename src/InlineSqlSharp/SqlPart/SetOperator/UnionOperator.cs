namespace InlineSqlSharp;

internal sealed class UnionOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Union)
        .PrependSpaceIf(_all, Keywords.All);
}
