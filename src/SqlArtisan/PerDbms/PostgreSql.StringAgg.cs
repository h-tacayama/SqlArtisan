using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Databases.PostgreSql;

// #88 string aggregation — the THIRD clause-shape sample: a divergent *function*
// (SELECT-list expression), not a fluent statement. Unlike UPSERT/MERGE there is
// NO wrapper state machine — the per-DBMS cost is one factory method returning a
// fixed-form node (depth 0). That is the key contrast measured in this step.
public static partial class Sql
{
    public static StringAggFunction StringAgg(object expr, object separator, object orderBy) =>
        new(Resolve(expr), Resolve(separator), Resolve(orderBy), Dbms.PostgreSql);
}
