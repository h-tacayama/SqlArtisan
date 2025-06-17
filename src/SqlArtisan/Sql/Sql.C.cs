using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause,
        params SearchedCaseWhenClause[] whenClauses) =>
        new([whenClause, .. whenClauses]);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause,
        CaseElseExpression elseExpr) =>
        new([whenClause], elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        SearchedCaseWhenClause whenClause6,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,
            whenClause6,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        SearchedCaseWhenClause whenClause6,
        SearchedCaseWhenClause whenClause7,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,
            whenClause6,
            whenClause7,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        SearchedCaseWhenClause whenClause6,
        SearchedCaseWhenClause whenClause7,
        SearchedCaseWhenClause whenClause8,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,
            whenClause6,
            whenClause7,
            whenClause8,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        SearchedCaseWhenClause whenClause6,
        SearchedCaseWhenClause whenClause7,
        SearchedCaseWhenClause whenClause8,
        SearchedCaseWhenClause whenClause9,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,
            whenClause6,
            whenClause7,
            whenClause8,
            whenClause9,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        SearchedCaseWhenClause whenClause4,
        SearchedCaseWhenClause whenClause5,
        SearchedCaseWhenClause whenClause6,
        SearchedCaseWhenClause whenClause7,
        SearchedCaseWhenClause whenClause8,
        SearchedCaseWhenClause whenClause9,
        SearchedCaseWhenClause whenClause10,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
            whenClause4,
            whenClause5,
            whenClause6,
            whenClause7,
            whenClause8,
            whenClause9,
            whenClause10,],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpression elseExpr) =>
        new(whenClauses, elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        params SimpleCaseWhenClause[] whenClauses) => new(
            Resolve(expr),
            whenClauses);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [whenClause],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        SimpleCaseWhenClause whenClause6,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
                whenClause6,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        SimpleCaseWhenClause whenClause6,
        SimpleCaseWhenClause whenClause7,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
                whenClause6,
                whenClause7,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        SimpleCaseWhenClause whenClause6,
        SimpleCaseWhenClause whenClause7,
        SimpleCaseWhenClause whenClause8,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
                whenClause6,
                whenClause7,
                whenClause8,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        SimpleCaseWhenClause whenClause6,
        SimpleCaseWhenClause whenClause7,
        SimpleCaseWhenClause whenClause8,
        SimpleCaseWhenClause whenClause9,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
                whenClause6,
                whenClause7,
                whenClause8,
                whenClause9,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause1,
        SimpleCaseWhenClause whenClause2,
        SimpleCaseWhenClause whenClause3,
        SimpleCaseWhenClause whenClause4,
        SimpleCaseWhenClause whenClause5,
        SimpleCaseWhenClause whenClause6,
        SimpleCaseWhenClause whenClause7,
        SimpleCaseWhenClause whenClause8,
        SimpleCaseWhenClause whenClause9,
        SimpleCaseWhenClause whenClause10,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [
                whenClause1,
                whenClause2,
                whenClause3,
                whenClause4,
                whenClause5,
                whenClause6,
                whenClause7,
                whenClause8,
                whenClause9,
                whenClause10,
            ],
            elseExpr);

    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause[] whenClauses,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            whenClauses,
            elseExpr);

    public static CoalesceFunction Coalesce(
        object primary,
        object secondary,
        params object[] others) => new(
            Resolve(primary),
            Resolve(secondary),
            Resolve(others));

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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentDateFunction CurrentDate => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentTimeFunction CurrentTime => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentTimestampFunction CurrentTimestamp => new();

    public static CurrvalFunction Currval(string sequenceName) =>
        new(sequenceName);
}
