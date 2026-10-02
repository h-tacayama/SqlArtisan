namespace SqlArtisan.Internal;

internal sealed class InnerJoinClause(TableReference table) : SqlPart, IJoinedRelation
{
    private readonly TableReference _table = table;

    TableReference IJoinedRelation.Relation => _table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Inner} {Keywords.Join} ")
        .Append(_table);
}
