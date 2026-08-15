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
        CaseElseExpression elseExpr)
    {
        CollectionGuard.ThrowIfEmpty(
            whenClauses,
            "CASE requires at least one WHEN clause.");

        return new(whenClauses, elseExpr);
    }

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
        params SimpleCaseWhenClause[] whenClauses)
    {
        CollectionGuard.ThrowIfEmpty(
            whenClauses,
            "CASE requires at least one WHEN clause.");

        return new(Resolve(expr), whenClauses);
    }

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
        CaseElseExpression elseExpr)
    {
        CollectionGuard.ThrowIfEmpty(
            whenClauses,
            "CASE requires at least one WHEN clause.");

        return new(Resolve(expr), whenClauses, elseExpr);
    }

    /// <summary>
    /// The ANSI <c>CAST(expr AS type)</c> expression. The target <paramref name="type"/>
    /// is emitted verbatim, so supply the exact SQL data type for your target database
    /// (for example <c>"VARCHAR2(10)"</c> on Oracle, <c>"NVARCHAR(10)"</c> on SQL Server).
    /// </summary>
    /// <returns>A <see cref="CastExpression"/> emitting <c>CAST(expr AS type)</c>.</returns>
    public static CastExpression Cast(object expr, string type) =>
        new(Resolve(expr), type);

    /// <summary>
    /// The <c>CEIL(expr)</c> function (smallest integer not less than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <remarks>
    /// MySQL, Oracle, PostgreSQL, and SQLite (3.35+) syntax — emitted verbatim
    /// as <c>CEIL</c>. SQL Server spells this function <c>CEILING</c>; use
    /// <see cref="Ceiling(object)"/> for that target. MySQL, PostgreSQL, and
    /// SQLite accept both spellings.
    /// </remarks>
    public static CeilFunction Ceil(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>CEILING(expr)</c> function (smallest integer not less than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <remarks>
    /// MySQL, PostgreSQL, SQLite (3.35+), and SQL Server syntax — emitted
    /// verbatim as <c>CEILING</c>. Oracle spells this function <c>CEIL</c>; use
    /// <see cref="Ceil(object)"/> for that target. MySQL, PostgreSQL, and
    /// SQLite accept both spellings.
    /// </remarks>
    public static CeilingFunction Ceiling(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>CHAR_LENGTH(<paramref name="source"/>)</c> function (the number of
    /// characters in a string).
    /// </summary>
    /// <param name="source">The string expression to measure.</param>
    /// <returns>A <see cref="CharLengthFunction"/> emitting <c>CHAR_LENGTH(source)</c>.</returns>
    /// <remarks>MySQL and PostgreSQL syntax. Oracle and SQLite count characters with
    /// <see cref="Length(object)"/>; SQL Server spells it <c>LEN</c>.</remarks>
    public static CharLengthFunction CharLength(object source) =>
        new(Resolve(source));

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
        params object[] others) =>
        new(ResolveVariadic(primary, secondary, others));

    /// <summary>
    /// The <c>CONCAT(<paramref name="primary"/>, <paramref name="secondary"/>)</c>
    /// function (the two arguments joined into a single string).
    /// </summary>
    /// <param name="primary">The first string expression.</param>
    /// <param name="secondary">The second string expression.</param>
    /// <returns>A <see cref="ConcatFunction"/> emitting <c>CONCAT(a, b)</c>.</returns>
    /// <remarks>Supported on every dialect (SQLite 3.44+). For three or more arguments —
    /// where Oracle's <c>CONCAT</c> differs — see
    /// <see cref="Concat(object, object, object, object[])"/>.</remarks>
    public static ConcatFunction Concat(object primary, object secondary) =>
        new(Resolve(primary), Resolve(secondary));

    /// <summary>
    /// The <c>CONCAT(<paramref name="primary"/>, <paramref name="secondary"/>, <paramref name="third"/>, ...)</c>
    /// function (three or more arguments joined into a single string).
    /// </summary>
    /// <param name="primary">The first string expression.</param>
    /// <param name="secondary">The second string expression.</param>
    /// <param name="third">The third string expression.</param>
    /// <param name="others">Any further string expressions, appended in order.</param>
    /// <returns>A <see cref="ConcatFunction"/> emitting <c>CONCAT(a, b, c, ...)</c>.</returns>
    /// <remarks>MySQL, PostgreSQL, SQLite (3.44+), and SQL Server syntax. Oracle's
    /// <c>CONCAT</c> takes exactly two arguments and rejects this three-or-more
    /// form — nest two-argument calls there instead, or use
    /// <see cref="DoublePipe(object, object, object[])"/> to chain any number without
    /// nesting (also valid on PostgreSQL and SQLite).</remarks>
    public static ConcatFunction Concat(
        object primary,
        object secondary,
        object third,
        params object[] others) =>
        new(ResolveVariadic(primary, secondary, third, others));

    /// <summary>
    /// The <c>CONCAT_WS(<paramref name="separator"/>, <paramref name="primary"/>,
    /// <paramref name="secondary"/>, ...)</c> function (the values joined with
    /// <paramref name="separator"/> between each pair, skipping any <c>NULL</c> value).
    /// </summary>
    /// <param name="separator">The separator placed between each value.</param>
    /// <param name="primary">The first value to join.</param>
    /// <param name="secondary">The second value to join.</param>
    /// <param name="others">Any further values to join, in order.</param>
    /// <returns>A <see cref="ConcatWsFunction"/> emitting
    /// <c>CONCAT_WS(sep, a, b, ...)</c>.</returns>
    /// <remarks>MySQL, PostgreSQL, SQLite (3.44+), and SQL Server (2017+) syntax. Oracle
    /// has no <c>CONCAT_WS</c> — join with <see cref="DoublePipe(object, object, object[])"/>
    /// and repeat the separator there.</remarks>
    public static ConcatWsFunction ConcatWs(
        object separator,
        object primary,
        object secondary,
        params object[] others) =>
        new(ResolveVariadic(separator, primary, secondary, others));

    /// <summary>
    /// Drops <paramref name="condition"/> out of a <c>WHERE</c> clause based on a
    /// runtime flag, without breaking the fluent chain: returns it when
    /// <paramref name="when"/> is <see langword="true"/>, otherwise an empty
    /// condition that emits nothing. A C#-side helper — not to be confused with
    /// <see cref="Sql.If(SqlCondition, object, object)"/> or
    /// <see cref="Sql.Iif(SqlCondition, object, object)"/>, which emit a SQL
    /// <c>IF</c>/<c>IIF</c> value expression.
    /// </summary>
    /// <param name="when">Whether to include <paramref name="condition"/>.</param>
    /// <param name="condition">The condition to include.</param>
    /// <returns><paramref name="condition"/>, or an empty <see cref="SqlCondition"/>.</returns>
    public static SqlCondition ConditionIf(bool when, SqlCondition condition) =>
        when ? condition : new EmptyCondition();

    /// <summary>
    /// The SQL Server full-text <c>CONTAINS(column, searchCondition)</c> predicate:
    /// matches rows whose <paramref name="column"/> satisfies the full-text
    /// <paramref name="searchCondition"/> (a word, prefix, or boolean combination).
    /// Requires a full-text index on the column.
    /// </summary>
    /// <param name="column">The full-text indexed column to search.</param>
    /// <param name="searchCondition">The full-text search condition (e.g.
    /// <c>"database"</c>, <c>"data AND query"</c>).</param>
    /// <returns>A <see cref="ContainsCondition"/> emitting
    /// <c>CONTAINS(column, searchCondition)</c>.</returns>
    /// <remarks>SQL Server syntax. Oracle's <c>CONTAINS</c> returns a numeric
    /// score instead — use <see cref="ContainsScore(object, object)"/> for that
    /// target.</remarks>
    public static ContainsCondition Contains(object column, object searchCondition) =>
        new(Resolve(column), Resolve(searchCondition));

    /// <summary>
    /// The Oracle Text <c>CONTAINS(column, query)</c> operator: the relevance score
    /// (0–100) of <paramref name="column"/> for <paramref name="query"/>, 0 when
    /// there is no match. Compare it in <c>WHERE</c> (e.g.
    /// <c>ContainsScore(...) &gt; 0</c>). Requires an Oracle Text index on the
    /// column.
    /// </summary>
    /// <param name="column">The Oracle Text indexed column to search.</param>
    /// <param name="query">The Oracle Text query (e.g. <c>"database"</c>,
    /// <c>"data % query"</c>).</param>
    /// <returns>A <see cref="ContainsScoreFunction"/> emitting
    /// <c>CONTAINS(column, query)</c>.</returns>
    /// <remarks>Oracle syntax. SQL Server's <c>CONTAINS</c> is a predicate
    /// instead — use <see cref="Contains(object, object)"/> for that
    /// target.</remarks>
    public static ContainsScoreFunction ContainsScore(object column, object query) =>
        new(Resolve(column), Resolve(query));

    /// <inheritdoc cref="ContainsScore(object, object)"/>
    /// <param name="column">The Oracle Text indexed column to search.</param>
    /// <param name="query">The Oracle Text query (e.g. <c>"database"</c>,
    /// <c>"data % query"</c>).</param>
    /// <param name="label">The label linking this operator to
    /// <see cref="Score(int)"/>, emitted as <c>CONTAINS(column, query, label)</c>.</param>
    public static ContainsScoreFunction ContainsScore(object column, object query, int label) =>
        new(Resolve(column), Resolve(query), label);

    /// <summary>
    /// The <c>(leftVector &lt;=&gt; rightVector)</c> cosine distance operator
    /// between two vectors (Oracle 23ai+, PostgreSQL). On PostgreSQL it requires
    /// the pgvector extension.
    /// </summary>
    /// <param name="leftVector">The first vector.</param>
    /// <param name="rightVector">The second vector.</param>
    /// <returns>A <c>&lt;=&gt;</c> operator expression.</returns>
    /// <remarks>
    /// Oracle (23ai+) and PostgreSQL syntax. On MySQL, <c>&lt;=&gt;</c> is the
    /// NULL-safe equality operator — valid SQL with silently different
    /// semantics, an equality test rather than a distance.
    /// </remarks>
    public static CosineDistanceOperator CosineDistance(object leftVector, object rightVector) =>
        new(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>COUNT(*)</c> aggregate function (the number of rows in the group,
    /// <c>NULL</c>s included).
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Count(object)"/>, which skips <c>NULL</c> values in the
    /// counted expression, <c>COUNT(*)</c> counts every row. Identical on every dialect.
    /// </remarks>
    /// <param name="asterisk">The <c>*</c> marker (<see cref="Sql.Asterisk"/>).</param>
    /// <returns>A <see cref="CountFunction"/> emitting <c>COUNT(*)</c>.</returns>
    public static CountFunction Count(AsteriskMarker asterisk) =>
        new(asterisk);

    /// <summary>
    /// The <c>COUNT(<paramref name="expr"/>)</c> aggregate function (the number of
    /// non-<c>NULL</c> values in the group).
    /// </summary>
    /// <param name="expr">The expression to count.</param>
    /// <returns>A <see cref="CountFunction"/> emitting <c>COUNT(expr)</c>.</returns>
    public static CountFunction Count(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Count(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// counting only distinct values.</param>
    /// <param name="expr">The expression to count.</param>
    /// <returns>A <see cref="CountFunction"/> emitting <c>COUNT(DISTINCT expr)</c>.</returns>
    public static CountFunction Count(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    /// <summary>
    /// The <c>CUBE(...)</c> GROUP BY grouping extension. Each element is an ordinary
    /// column or a <c>Sql.Group(...)</c> composite column (so
    /// <c>Cube(Group(a, b), c)</c> emits <c>CUBE((a, b), c)</c>). Emitted as
    /// <c>CUBE(a, b)</c>.
    /// </summary>
    /// <remarks>Not supported by MySQL or SQLite; emitted as written for the
    /// database to reject.</remarks>
    public static CubeGrouping Cube(object element, params object[] elements) =>
        new(GroupByItemResolver.ResolveElements(element, elements));

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
    /// <param name="sequenceName">The name of the sequence to read.</param>
    /// <returns>A <see cref="CurrvalFunction"/> emitting
    /// <c>CURRVAL('sequenceName')</c>.</returns>
    /// <remarks>
    /// Dialect-specific (PostgreSQL). For Oracle use <see cref="Sequence(string)"/>
    /// with <c>.Currval</c>.
    /// </remarks>
    public static CurrvalFunction Currval(string sequenceName) =>
        new(sequenceName);
}
