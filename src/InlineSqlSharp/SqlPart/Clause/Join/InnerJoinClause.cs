namespace InlineSqlSharp;

internal sealed class InnerJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Inner)
        .AppendSpace(Keywords.Join)
        .Append(_table);
}
