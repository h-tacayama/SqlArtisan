namespace InlineSqlSharp;

public sealed class NotExistsCondition : AbstractCondition
{
    private readonly SqlPartAgent _subquery;

    internal NotExistsCondition(ISubquery subquery)
    {
        _subquery = new(subquery.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} {Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
