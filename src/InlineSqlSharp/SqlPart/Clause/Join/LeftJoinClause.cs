namespace InlineSqlSharp;

internal sealed class LeftJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.LEFT)
        .AppendSpace(Keywords.JOIN)
        .Append(_table);
}
