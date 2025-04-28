namespace InlineSqlSharp;

internal sealed class LeftJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Left)
        .AppendSpace(Keywords.Join)
        .Append(_table);
}
