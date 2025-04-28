namespace InlineSqlSharp;

internal sealed class WhereClause(AbstractCondition condition) : AbstractSqlPart
{
    private readonly AbstractCondition _condition = condition;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Where)
        .Append(_condition);
}
