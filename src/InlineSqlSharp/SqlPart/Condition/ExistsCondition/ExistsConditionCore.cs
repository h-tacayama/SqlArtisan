namespace InlineSqlSharp;

internal sealed class ExistsConditionCore(bool isNot, ISubquery subquery)
{
    private readonly bool _isNot = isNot;
    private readonly SqlPartAgent _subquery = new(subquery.FormatSql);

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.EXISTS)
        .EncloseInParentheses(_subquery);
}
