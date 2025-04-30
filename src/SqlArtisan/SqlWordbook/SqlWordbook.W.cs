using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static SearchedCaseWhenCondition When(
        AbstractCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpr When(object whenExpr) =>
        new(Resolve(whenExpr));
}
