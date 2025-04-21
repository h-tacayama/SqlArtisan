namespace InlineSqlSharp;

internal sealed class FromClause(AbstractTableReference[] tables) :
    AbstractSqlPart
{
    private readonly AbstractTableReference[] _tables = tables;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.FROM)
        .AppendCsv(_tables);
}
