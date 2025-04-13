namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static CharacterSearchedCaseExpr CASE(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr<CharacterExpr> elseExpr) =>
        new(whenClauses, elseExpr);

    public static DateTimeSearchedCaseExpr CASE(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr<DateTimeExpr> elseExpr) =>
        new(whenClauses, elseExpr);

    public static NumericSearchedCaseExpr CASE(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpr<NumericExpr> elseExpr) =>
        new(whenClauses, elseExpr);

    public static CharacterSimpleCaseExpr CASE(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr<CharacterExpr> elseExpr) =>
        new(expr, whenClauses, elseExpr);

    public static DateTimeSimpleCaseExpr CASE(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr<DateTimeExpr> elseExpr) =>
        new(expr, whenClauses, elseExpr);

    public static NumericSimpleCaseExpr CASE(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpr<NumericExpr> elseExpr) =>
        new(expr, whenClauses, elseExpr);

    public static ConcatFunction CONCAT(
        CharacterExpr primary,
        CharacterExpr secondary,
        params CharacterExpr[] others) => new(primary, secondary, others);

    public static CountFunction COUNT(IExpr expr) => new(AllOrDistinct.All, expr);

    public static CountFunction COUNT(AllOrDistinct allOrDistinct, IExpr expr) =>
        new(allOrDistinct, expr);

    public static AnalyticCumeDistFunction CUME_DIST() => new();
}
