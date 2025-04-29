namespace InlineSqlSharp;

internal sealed class FullJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Full} {Keywords.Join} ")
        .Append(_table);
}
