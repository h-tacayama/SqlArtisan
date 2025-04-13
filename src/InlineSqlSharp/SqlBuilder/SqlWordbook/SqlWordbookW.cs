namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static SearchedCaseWhenCondition WHEN(ICondition whenCondition) =>
        new(whenCondition);

    public static SimpleCaseWhenExpr WHEN(object whenExpr) => new(whenExpr);
}
