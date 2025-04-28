namespace InlineSqlSharp;

internal sealed class OnClause(AbstractCondition condition) : AbstractSqlPart
{
    private readonly AbstractCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.On)
        .Append(_condition);
}
