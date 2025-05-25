using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    public static OrderByClause OrderBy(
        params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
