using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static SearchedCaseExpr CASE(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr elseExpr) =>
        new(whenClauses, elseExpr);

    public static SimpleCaseExpr CASE(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr elseExpr) => new(
            Resolve(expr),
            whenClauses,
            elseExpr);

    public static ConcatFunction CONCAT(
        object primary,
        object secondary,
        params object[] others) => new(
            Resolve(primary),
            Resolve(secondary),
            Resolve(others));

    public static CountFunction COUNT(object expr) =>
        new(Resolve(expr));

    public static CountFunctionWithDistinct COUNT(Distinct distinct, object expr) =>
        new(distinct, Resolve(expr));

    public static AnalyticCumeDistFunction CUME_DIST() => new();
}
