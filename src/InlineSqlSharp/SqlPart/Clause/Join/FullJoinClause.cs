namespace InlineSqlSharp;

internal sealed class FullJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Full)
        .AppendSpace(Keywords.Join)
        .Append(_table);
}
