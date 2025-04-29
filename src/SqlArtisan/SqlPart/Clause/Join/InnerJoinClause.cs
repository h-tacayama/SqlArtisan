namespace SqlArtisan;

internal sealed class InnerJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Inner} {Keywords.Join} ")
        .Append(_table);
}
