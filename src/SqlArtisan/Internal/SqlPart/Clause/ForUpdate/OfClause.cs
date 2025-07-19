namespace SqlArtisan.Internal;

public sealed class OfClause(DbColumn tableIdentifier) : SqlPart
{
    private readonly DbColumn _tableIdentifier = tableIdentifier;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Of)
        .PrependSpace(_tableIdentifier);
}
