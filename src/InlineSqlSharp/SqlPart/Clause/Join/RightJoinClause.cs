namespace InlineSqlSharp;

internal sealed class RightJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Right} {Keywords.Join} ")
        .Append(_table);
}
