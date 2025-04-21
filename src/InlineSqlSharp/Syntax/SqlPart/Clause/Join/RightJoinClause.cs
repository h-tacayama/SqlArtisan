namespace InlineSqlSharp;

internal sealed class RightJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly JoinClauseCore _core = new(Keywords.RIGHT, table);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
