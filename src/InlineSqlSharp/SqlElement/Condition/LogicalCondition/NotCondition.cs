namespace InlineSqlSharp;

public sealed class NotCondition(ICondition condition) : ICondition
{
    private readonly ICondition _condition = condition;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.NOT)
        .EncloseInParentheses(_condition);
}
