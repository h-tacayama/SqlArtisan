using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static SearchedCaseWhenCondition WHEN(
        AbstractCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpr WHEN(object whenExpr) =>
        new(Resolve(whenExpr));
}
