using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static WaitBehavior Wait(int seconds) => new(seconds);

    public static SearchedCaseWhenCondition When(
        SqlCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpression When(object whenExpr) =>
        new(Resolve(whenExpr));

    public static IWithBuilderWith With(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithClause(ctes));

    public static IWithBuilderWith WithRecursive(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithRecursiveClause(ctes));
}
