namespace InlineSqlSharp;

internal sealed class FullJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.FULL)
        .AppendSpace(Keywords.JOIN)
        .Append(_table);
}
