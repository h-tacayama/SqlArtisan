namespace SqlArtisan.Internal;

internal sealed class NaturalRightJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Natural} {Keywords.Right} {Keywords.Join} ")
        .Append(_table);
}
