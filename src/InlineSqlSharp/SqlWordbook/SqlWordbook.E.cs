using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static CaseElseExpr Else(object thenExpr) =>
        new(Resolve(thenExpr));

    public static ExistsCondition Exists(ISubquery subquery) => new(subquery);
}
