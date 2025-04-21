namespace InlineSqlSharp;

internal sealed class LeftJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly JoinClauseCore _core = new(Keywords.LEFT, table);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
