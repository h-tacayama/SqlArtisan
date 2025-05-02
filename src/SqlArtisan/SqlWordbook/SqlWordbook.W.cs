using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static SearchedCaseWhenCondition When(
        SqlCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpression When(object whenExpr) =>
        new(Resolve(whenExpr));
}
