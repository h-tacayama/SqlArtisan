using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static SearchedCaseWhenCondition When(
        AbstractCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpr When(object whenExpr) =>
        new(Resolve(whenExpr));
}
