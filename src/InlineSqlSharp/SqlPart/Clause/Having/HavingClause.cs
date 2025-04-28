namespace InlineSqlSharp;

internal sealed class HavingClause(AbstractCondition condition) : AbstractSqlPart
{
    private readonly AbstractCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Having)
        .Append(_condition);
}
