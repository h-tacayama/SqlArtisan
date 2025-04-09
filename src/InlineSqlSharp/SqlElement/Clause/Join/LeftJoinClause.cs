namespace InlineSqlSharp;

internal sealed class LeftJoinClause(ITableReference table) : ISqlElement
{
    private readonly JoinClauseCore _core = new(Keywords.LEFT, table);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
