namespace InlineSqlSharp;

public sealed class ExistsCondition : AbstractCondition
{
    private readonly SqlPartAgent _subquery;

    internal ExistsCondition(ISubquery subquery)
    {
        _subquery = new(subquery.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.EXISTS)
        .EncloseInParentheses(_subquery);
}
