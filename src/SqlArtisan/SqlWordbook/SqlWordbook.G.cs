using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));
}
