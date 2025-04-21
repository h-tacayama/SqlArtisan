using static InlineSqlSharp.ExprRsolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static CaseElseExpr ELSE(object thenExpr) =>
        new(Resolve(thenExpr));

    public static ExistsCondition EXISTS(ISubquery subquery) => new(subquery);
}
