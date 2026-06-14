using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Databases.MySql;

// MySQL's GROUP_CONCAT: inline ORDER BY + SEPARATOR literal. The separator is a
// C# string (not object) because it must be emitted as a literal, not a bind
// parameter — the signature itself encodes the divergence.
public static partial class Sql
{
    public static GroupConcatFunction GroupConcat(object expr, object orderBy, string separator) =>
        new(Resolve(expr), Resolve(orderBy), separator, Dbms.MySql);
}
