namespace SqlArtisan;

internal sealed class RightJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Right} {Keywords.Join} ")
        .Append(_table);
}
