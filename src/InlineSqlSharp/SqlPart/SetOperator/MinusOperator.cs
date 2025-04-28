namespace InlineSqlSharp;

internal sealed class MinusOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Minus)
        .PrependSpaceIf(_all, Keywords.All);
}
