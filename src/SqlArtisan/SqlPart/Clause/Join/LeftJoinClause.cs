namespace SqlArtisan;

internal sealed class LeftJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Left} {Keywords.Join} ")
        .Append(_table);
}
