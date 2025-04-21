namespace InlineSqlSharp;

internal sealed class FullJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly JoinClauseCore _core = new(Keywords.FULL, table);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
