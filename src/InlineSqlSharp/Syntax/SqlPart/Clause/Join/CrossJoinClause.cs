namespace InlineSqlSharp;

internal sealed class CrossJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly JoinClauseCore _core = new(Keywords.CROSS, table);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
