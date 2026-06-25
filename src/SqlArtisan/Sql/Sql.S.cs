using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Begins a <c>SELECT</c> statement projecting <paramref name="selectItems"/>.
    /// Continue with <c>.From(...)</c> and the remaining clauses. Each item is a
    /// column, expression, or <c>expr.As("alias")</c>.
    /// </summary>
    /// <param name="selectItems">The columns or expressions to project.</param>
    /// <returns>A select builder positioned for <c>.From(...)</c>.</returns>
    public static ISelectBuilderSelect Select(
        params object[] selectItems) =>
        new SelectBuilder(SelectClause.Parse(selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Distinct"/>), emitting <c>SELECT DISTINCT</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithDistinct.Parse(
                distinct,
                selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints (<see cref="Hints(string)"/>), emitted after <c>SELECT</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithHints.Parse(
                hints,
                selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints (<see cref="Hints(string)"/>), emitted after <c>SELECT</c>.</param>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Distinct"/>), emitting <c>SELECT ... DISTINCT</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithOptions.Parse(
                hints,
                distinct,
                selectItems));

    /// <summary>
    /// Wraps a <c>GROUP_CONCAT</c> separator in MySQL's <c>SEPARATOR</c> keyword
    /// form, distinguishing it from SQLite's positional separator argument.
    /// MySQL requires a string literal here, so <paramref name="separator"/> is
    /// emitted inline as an escaped literal rather than a bind parameter.
    /// </summary>
    public static SeparatorClause Separator(string separator) =>
        new(separator);

    /// <summary>
    /// Creates a reference to a sequence using the Oracle dotted syntax,
    /// e.g. <c>name.NEXTVAL</c> / <c>name.CURRVAL</c> via <c>.Nextval</c> / <c>.Currval</c>.
    /// </summary>
    /// <remarks>
    /// Dialect-specific (Oracle). For PostgreSQL use <see cref="Nextval(string)"/> /
    /// <see cref="Currval(string)"/>; for SQL Server use <see cref="NextValueFor(string)"/>.
    /// </remarks>
    public static DbSequence Sequence(string name) => new(name);

    /// <summary>
    /// The <c>SIGN(expr)</c> function: the sign of <paramref name="expr"/>
    /// (-1, 0, or 1).
    /// </summary>
    public static SignFunction Sign(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>SKIP LOCKED</c> behavior for a <c>FOR UPDATE</c> clause: skip rows that
    /// are already locked instead of blocking on them.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SkipLockedBehavior SkipLocked => new();

    /// <summary>
    /// The <c>SQRT(expr)</c> function: the square root of <paramref name="expr"/>.
    /// </summary>
    public static SqrtFunction Sqrt(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STRING_AGG(expr, separator)</c> string aggregate (PostgreSQL and
    /// SQL Server). Order the values per dialect: pass <c>OrderBy(...)</c> as an
    /// argument for PostgreSQL's inline form, or chain
    /// <c>.WithinGroup(OrderBy(...))</c> for SQL Server's <c>WITHIN GROUP</c> form.
    /// </summary>
    public static StringAggFunction StringAgg(object expr, object separator) =>
        new(Resolve(expr), Resolve(separator));

    /// <summary>
    /// The <c>STRING_AGG(expr, separator ORDER BY ...)</c> string aggregate with
    /// PostgreSQL's inline ordering (the <c>ORDER BY</c> sits inside the call).
    /// </summary>
    public static StringAggFunction StringAgg(
        object expr,
        object separator,
        OrderByClause orderByClause) => new(Resolve(expr), Resolve(separator), orderByClause);

    /// <summary>
    /// The <c>SUBSTR(source, position)</c> function: the substring of
    /// <paramref name="source"/> from the 1-based <paramref name="position"/> to the
    /// end.
    /// </summary>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position.</param>
    /// <returns>A <c>SUBSTR</c> function expression.</returns>
    public static SubstrFunction Substr(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    /// <inheritdoc cref="Substr(object, object)"/>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position.</param>
    /// <param name="length">The number of characters to take.</param>
    public static SubstrFunction Substr(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    /// <summary>
    /// The <c>SUBSTRB(source, position)</c> function: like <see cref="Substr(object, object)"/>
    /// but with <paramref name="position"/> measured in bytes.
    /// </summary>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position, in bytes.</param>
    /// <returns>A <c>SUBSTRB</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static SubstrbFunction Substrb(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    /// <inheritdoc cref="Substrb(object, object)"/>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position, in bytes.</param>
    /// <param name="length">The number of bytes to take.</param>
    public static SubstrbFunction Substrb(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    /// <summary>
    /// The <c>SUM(expr)</c> aggregate: the total of <paramref name="expr"/> over the
    /// group.
    /// </summary>
    /// <param name="expr">The numeric expression to total.</param>
    /// <returns>A <c>SUM</c> aggregate expression.</returns>
    public static SumFunction Sum(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Sum(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Distinct"/>), emitting <c>SUM(DISTINCT expr)</c>.</param>
    /// <param name="expr">The numeric expression to total.</param>
    public static SumFunction Sum(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    /// <summary>
    /// The <c>SYSDATE</c> function. Dialect-specific (Oracle). For the standard
    /// current date/time use <see cref="CurrentTimestamp"/> or <see cref="CurrentDate"/>.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction Sysdate => new();

    /// <summary>
    /// The <c>SYSTIMESTAMP</c> function. Dialect-specific (Oracle). For the standard
    /// current timestamp use <see cref="CurrentTimestamp"/>.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction Systimestamp => new();
}
