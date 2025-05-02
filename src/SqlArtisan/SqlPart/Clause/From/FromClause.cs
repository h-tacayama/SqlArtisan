namespace SqlArtisan;

internal sealed class FromClause(TableReference[] tables) : SqlPart
{
    private readonly TableReference[] _tables = tables;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.From} ")
        .AppendCsv(_tables);
}
