namespace SqlArtisan;

internal sealed class InnerJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Inner} {Keywords.Join} ")
        .Append(_table);
}
