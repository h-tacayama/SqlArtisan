using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static CaseElseExpression Else(object thenExpr) =>
        new(Resolve(thenExpr));

    public static ExistsCondition Exists(ISubquery subquery) => new(subquery);
}
