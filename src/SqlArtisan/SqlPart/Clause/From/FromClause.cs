namespace SqlArtisan;

internal sealed class FromClause(AbstractTableReference[] tables) :
    AbstractSqlPart
{
    private readonly AbstractTableReference[] _tables = tables;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.From} ")
        .AppendCsv(_tables);
}
