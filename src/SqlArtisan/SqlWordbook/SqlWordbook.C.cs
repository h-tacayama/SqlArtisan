using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static SearchedCaseExpr Case(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr elseExpr) =>
        new(whenClauses, elseExpr);

    public static SimpleCaseExpr Case(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr elseExpr) => new(
            Resolve(expr),
            whenClauses,
            elseExpr);

    public static ConcatFunction Concat(
        object primary,
        object secondary,
        params object[] others) => new(
            Resolve(primary),
            Resolve(secondary),
            Resolve(others));

    public static CountFunction Count(object expr) =>
        new(Resolve(expr));

    public static CountFunction Count(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    public static AnalyticCumeDistFunction CumeDist() => new();
}
