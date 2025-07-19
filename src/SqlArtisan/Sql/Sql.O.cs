using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    public static OfClause Of(DbColumn tableIdentifier) => new(tableIdentifier);

    public static OrderByClause OrderBy(
        params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
