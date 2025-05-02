namespace SqlArtisan;

internal sealed class FullJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Full} {Keywords.Join} ")
        .Append(_table);
}
