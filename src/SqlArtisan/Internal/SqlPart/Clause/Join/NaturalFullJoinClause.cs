namespace SqlArtisan.Internal;

internal sealed class NaturalFullJoinClause(TableReference table) : SqlPart, IJoinedRelation
{
    private readonly TableReference _table = table;

    TableReference IJoinedRelation.Relation => _table;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Natural} {Keywords.Full} {Keywords.Join} ")
        .Append(_table);
}
