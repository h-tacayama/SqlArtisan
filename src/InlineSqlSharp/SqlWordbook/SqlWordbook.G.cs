using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static GreatestFunction Greatest(params object[] expressions) =>
        new(Resolve(expressions));
}
