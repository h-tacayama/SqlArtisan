using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Databases.SqlServer;

// SQL Server also spells it STRING_AGG, but uses the WITHIN GROUP form — so it
// maps to a DIFFERENT node than PostgreSQL's same-named function. Same name,
// different structure: proof that sharing by name alone is impossible.
public static partial class Sql
{
    public static WithinGroupAggFunction StringAgg(object expr, object separator, object orderBy) =>
        new(Keywords.StringAgg, Resolve(expr), Resolve(separator), Resolve(orderBy), Dbms.SqlServer);
}
