namespace InlineSqlSharp;

internal sealed class CrossJoinClause(AbstractTableReference table) :
    AbstractSqlPart
{
    private readonly AbstractTableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Cross)
        .AppendSpace(Keywords.Join)
        .Append(_table);
}
