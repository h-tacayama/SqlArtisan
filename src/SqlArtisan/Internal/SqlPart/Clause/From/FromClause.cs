namespace SqlArtisan.Internal;

internal sealed class FromClause(TableReference[] tables) : SqlPart
{
    private readonly TableReference[] _tables = tables;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.From} ")
        .AppendCsv(_tables);
}
