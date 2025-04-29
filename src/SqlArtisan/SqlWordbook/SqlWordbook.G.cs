using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));
}
