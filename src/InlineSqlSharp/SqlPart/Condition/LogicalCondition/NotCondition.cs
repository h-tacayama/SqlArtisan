namespace InlineSqlSharp;

public sealed class NotCondition(AbstractCondition condition) :
    AbstractCondition
{
    private readonly AbstractCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.NOT)
        .EncloseInParentheses(_condition);
}
