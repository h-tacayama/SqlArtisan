using static InlineSqlSharp.ExprRsolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static GreatestFunction GREATEST(params object[] expressions) =>
        new(Resolve(expressions));
}
