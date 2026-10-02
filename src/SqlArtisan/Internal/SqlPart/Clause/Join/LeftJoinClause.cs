namespace SqlArtisan.Internal;

internal sealed class LeftJoinClause(TableReference table) : SqlPart, IJoinedRelation
{
    private readonly TableReference _table = table;

    TableReference IJoinedRelation.Relation => _table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Left} {Keywords.Join} ")
        .Append(_table);
}
