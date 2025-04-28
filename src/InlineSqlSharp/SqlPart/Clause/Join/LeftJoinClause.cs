namespace InlineSqlSharp;

internal sealed class LeftJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Left} {Keywords.Join} ")
        .Append(_table);
}
