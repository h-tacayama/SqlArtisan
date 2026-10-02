namespace SqlArtisan.Internal;

internal sealed class RightJoinClause(TableReference table) : SqlPart, IJoinedRelation
{
    private readonly TableReference _table = table;

    TableReference IJoinedRelation.Relation => _table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Right} {Keywords.Join} ")
        .Append(_table);
}
