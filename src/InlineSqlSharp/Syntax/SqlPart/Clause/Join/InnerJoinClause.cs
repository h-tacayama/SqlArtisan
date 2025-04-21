namespace InlineSqlSharp;

internal sealed class InnerJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly JoinClauseCore _core = new(Keywords.INNER, table);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
