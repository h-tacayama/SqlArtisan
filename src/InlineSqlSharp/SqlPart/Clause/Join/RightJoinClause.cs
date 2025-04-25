namespace InlineSqlSharp;

internal sealed class RightJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.RIGHT)
        .AppendSpace(Keywords.JOIN)
        .Append(_table);
}
