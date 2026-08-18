namespace SqlArtisan.Internal;

public sealed class OfClause : SqlPart
{
    private readonly DbColumn _tableIdentifier;

    internal OfClause(DbColumn tableIdentifier) => _tableIdentifier = tableIdentifier;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Of)
        .PrependSpace(_tableIdentifier);
}
