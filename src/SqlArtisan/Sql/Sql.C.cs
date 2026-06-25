using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The searched <c>CASE WHEN ... THEN ... [ELSE ...] END</c> expression. Build each
    /// branch with <c>Sql.When(condition).Then(result)</c> and an optional
    /// <c>Sql.Else(result)</c>.
    /// </summary>
    /// <param name="whenClause">The first <c>WHEN ... THEN ...</c> branch.</param>
    /// <param name="whenClauses">Any additional <c>WHEN ... THEN ...</c> branches.</param>
    /// <returns>A <see cref="SearchedCaseExpression"/> emitting
    /// <c>CASE WHEN ... THEN ... END</c>.</returns>
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause,
        params SearchedCaseWhenClause[] whenClauses) =>
        new([whenClause, .. whenClauses]);

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause,
        CaseElseExpression elseExpr) =>
        new([whenClause], elseExpr);

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause whenClause1,
        SearchedCaseWhenClause whenClause2,
        CaseElseExpression elseExpr) =>
        new([
            whenClause1,
            whenClause2,
        ],
            elseExpr);

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(SearchedCaseWhenClause, SearchedCaseWhenClause[])"/>
    public static SearchedCaseExpression Case(
        SearchedCaseWhenClause[] whenClauses,
        CaseElseExpression elseExpr) =>
        new(whenClauses, elseExpr);

    /// <summary>
    /// The simple <c>CASE <paramref name="expr"/> WHEN ... THEN ... [ELSE ...] END</c>
    /// expression, comparing <paramref name="expr"/> against each branch's value. Build
    /// each branch with <c>Sql.When(value).Then(result)</c> and an optional
    /// <c>Sql.Else(result)</c>.
    /// </summary>
    /// <param name="expr">The expression compared against each <c>WHEN</c> value.</param>
    /// <param name="whenClauses">The <c>WHEN ... THEN ...</c> branches.</param>
    /// <returns>A <see cref="SimpleCaseExpression"/> emitting
    /// <c>CASE expr WHEN ... THEN ... END</c>.</returns>
    public static SimpleCaseExpression Case(
        object expr,
        params SimpleCaseWhenClause[] whenClauses) => new(
            Resolve(expr),
            whenClauses);

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
    public static SimpleCaseExpression Case(
        object expr,
        SimpleCaseWhenClause whenClause,
        CaseElseExpression elseExpr) => new(
            Resolve(expr),
            [whenClause],
            elseExpr);

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <inheritdoc cref="Case(object, SimpleCaseWhenClause[])"/>
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

    /// <summary>
    /// The <c>COALESCE(<paramref name="primary"/>, <paramref name="secondary"/>, ...)</c>
    /// function (the first non-<c>NULL</c> argument).
    /// </summary>
    /// <param name="primary">The first candidate value.</param>
    /// <param name="secondary">The second candidate value, used when
    /// <paramref name="primary"/> is <c>NULL</c>.</param>
    /// <param name="others">Any further candidate values, tried in order.</param>
    /// <returns>A <see cref="CoalesceFunction"/> emitting
    /// <c>COALESCE(a, b, ...)</c>.</returns>
    public static CoalesceFunction Coalesce(
        object primary,
        object secondary,
        params object[] others) => new(
            Resolve(primary),
            Resolve(secondary),
            Resolve(others));

    /// <summary>
    /// The <c>CONCAT(<paramref name="primary"/>, <paramref name="secondary"/>, ...)</c>
    /// function (the arguments joined into a single string).
    /// </summary>
    /// <param name="primary">The first string expression.</param>
    /// <param name="secondary">The second string expression.</param>
    /// <param name="others">Any further string expressions, appended in order.</param>
    /// <returns>A <see cref="ConcatFunction"/> emitting <c>CONCAT(a, b, ...)</c>.</returns>
    public static ConcatFunction Concat(
        object primary,
        object secondary,
        params object[] others) => new(
            Resolve(primary),
            Resolve(secondary),
            Resolve(others));

    /// <summary>
    /// The <c>COUNT(<paramref name="expr"/>)</c> aggregate function (the number of
    /// non-<c>NULL</c> values in the group).
    /// </summary>
    /// <param name="expr">The expression to count.</param>
    /// <returns>A <see cref="CountFunction"/> emitting <c>COUNT(expr)</c>.</returns>
    public static CountFunction Count(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Count(object)"/>
    public static CountFunction Count(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    /// <summary>
    /// The <c>CUBE(...)</c> GROUP BY grouping extension. Each element is an ordinary
    /// column or a <c>Sql.Group(...)</c> composite column (so
    /// <c>Cube(Group(a, b), c)</c> emits <c>CUBE((a, b), c)</c>). Emitted as
    /// <c>CUBE(a, b)</c>. MySQL and SQLite have no CUBE; <c>Build</c> still emits it
    /// faithfully rather than rejecting it, leaving the unsupported statement for
    /// the target database to reject.
    /// </summary>
    public static CubeGrouping Cube(object element, params object[] elements) =>
        new(GroupByItemResolver.ResolveElements([element, .. elements]));

    /// <summary>
    /// The <c>CUME_DIST()</c> analytic function (cumulative distribution of the current
    /// row within its window). Complete it with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>An <see cref="AnalyticCumeDistFunction"/> emitting <c>CUME_DIST()</c>.</returns>
    public static AnalyticCumeDistFunction CumeDist() => new();

    /// <summary>
    /// The <c>CURRENT_DATE</c> function (the current date).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentDateFunction CurrentDate => new();

    /// <summary>
    /// The <c>CURRENT ROW</c> window-frame bound.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static FrameBound CurrentRow => FrameBound.CurrentRow();

    /// <summary>
    /// The <c>CURRENT_TIME</c> function (the current time of day).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static CurrentTimeFunction CurrentTime => new();

    /// <summary>
    /// The <c>CURRENT_TIMESTAMP</c> function (the current date and time).
    /// </summary>
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
