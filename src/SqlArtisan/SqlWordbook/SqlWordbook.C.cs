using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpression elseExpr) =>
        new(whenClauses, elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpression elseExpr) => new(
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
