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
            whenClause2,
        ],
            elseExpr);

    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        SearchedCaseWhenClause whenClause3,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
            whenClause3,
        ],
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
            whenClause4,
        ],
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
            whenClause5,
        ],
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
            whenClause6,
        ],
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
            whenClause7,
        ],
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
            whenClause8,
        ],
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
            whenClause9,
        ],
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
            whenClause10,
        ],
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

    /// <summary>
    /// The ANSI <c>CAST(expr AS type)</c> expression. The target <paramref name="type"/>
    /// is emitted verbatim, so supply the exact SQL data type for your target database
    /// (for example <c>"VARCHAR2(10)"</c> on Oracle, <c>"NVARCHAR(10)"</c> on SQL Server).
    /// </summary>
    public static CastExpression Cast(object expr, string type) =>
        new(Resolve(expr), type);

    /// <summary>
    /// The <c>CEIL(expr)</c> function (smallest integer not less than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <remarks>
    /// Emitted verbatim as <c>CEIL</c> on every DBMS. SQL Server spells this
    /// function <c>CEILING</c>; use <see cref="Ceiling(object)"/> for that
    /// target. MySQL and PostgreSQL accept both spellings.
    /// </remarks>
    public static CeilFunction Ceil(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>CEILING(expr)</c> function (smallest integer not less than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <remarks>
    /// Emitted verbatim as <c>CEILING</c> on every DBMS. Oracle and SQLite spell
    /// this function <c>CEIL</c>; use <see cref="Ceil(object)"/> for those
    /// targets. MySQL and PostgreSQL accept both spellings.
    /// </remarks>
    public static CeilingFunction Ceiling(object expr) =>
        new(Resolve(expr));

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

    /// <summary>
    /// The <c>CUBE(...)</c> GROUP BY grouping extension. Each element is an ordinary
    /// column or a <c>Sql.Group(...)</c> composite column (so
    /// <c>Cube(Group(a, b), c)</c> emits <c>CUBE((a, b), c)</c>). Emitted as
    /// <c>CUBE(a, b)</c>. MySQL and SQLite have no CUBE, but <c>Build</c> emits it
    /// faithfully regardless — DBMS availability is the analyzer's concern
    /// (ADR 0003), not a build-time check.
    /// </summary>
    public static CubeGrouping Cube(object element, params object[] elements) =>
        new(GroupByItemResolver.ResolveElements([element, .. elements]));

    public static AnalyticCumeDistFunction CumeDist() => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentDateFunction CurrentDate => new();

    /// <summary>
    /// The <c>CURRENT ROW</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound CurrentRow => FrameBound.CurrentRow();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentTimeFunction CurrentTime => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentTimestampFunction CurrentTimestamp => new();

    /// <summary>
    /// Gets the current value of a sequence using the PostgreSQL syntax
    /// <c>CURRVAL('sequenceName')</c>.
    /// </summary>
    /// <remarks>
    /// Dialect-specific (PostgreSQL). For Oracle use <see cref="Sequence(string)"/>
    /// with <c>.Currval</c>.
    /// </remarks>
    public static CurrvalFunction Currval(string sequenceName) =>
        new(sequenceName);
}
