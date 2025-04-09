namespace InlineSqlSharp;

public sealed class ExistsConditionCore(bool isNot, ISubquery subquery)
{
    private readonly bool _isNot = isNot;
    private readonly ISubquery _subquery = subquery;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIf(_isNot, Keywords.NOT)
        .AppendSpace(Keywords.EXISTS)
        .EncloseInParentheses(_subquery);
}
