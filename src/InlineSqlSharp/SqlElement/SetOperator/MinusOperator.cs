namespace InlineSqlSharp;

public sealed class MinusOperator(bool all) : ISqlElement
{
    private readonly bool _all = all;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.MINUS)
        .PrependSpaceIf(_all, Keywords.ALL);
}
