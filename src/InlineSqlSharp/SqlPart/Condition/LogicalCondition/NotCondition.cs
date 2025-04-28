namespace InlineSqlSharp;

public sealed class NotCondition : AbstractCondition
{
    private readonly AbstractCondition _condition;

    internal NotCondition(AbstractCondition condition)
    {
        _condition = condition;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Not)
        .EncloseInParentheses(_condition);
}
