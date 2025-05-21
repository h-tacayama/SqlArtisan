namespace SqlArtisan.Internal;

internal sealed class CrossJoinClause(TableReference table) : SqlPart
{
    private readonly TableReference _table = table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Cross} {Keywords.Join} ")
        .Append(_table);
}
