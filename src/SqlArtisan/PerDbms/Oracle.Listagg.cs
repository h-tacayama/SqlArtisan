using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Databases.Oracle;

// Oracle's LISTAGG shares SQL Server's WITHIN GROUP structure (same node, name
// passed in) but is named differently — so the namespace exposes Listagg, not
// StringAgg. The naming divergence is invisible in one node, visible in the API.
public static partial class Sql
{
    public static WithinGroupAggFunction Listagg(object expr, object separator, object orderBy) =>
        new(Keywords.Listagg, Resolve(expr), Resolve(separator), Resolve(orderBy), Dbms.Oracle);
}
