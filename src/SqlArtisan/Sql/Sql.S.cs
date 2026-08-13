using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The Oracle Text <c>SCORE(label)</c> function: the relevance score computed
    /// by the <see cref="ContainsScore(object, object, int)"/> operator carrying
    /// the same <paramref name="label"/>, for a select list or <c>ORDER BY</c>.
    /// </summary>
    /// <param name="label">The label of the <c>CONTAINS</c> operator whose score
    /// to read.</param>
    /// <returns>A <see cref="ScoreFunction"/> emitting <c>SCORE(label)</c>.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ScoreFunction Score(int label) => new(label);

    /// <summary>
    /// The <c>SECOND</c> interval field, for the sole-field overload of
    /// <see cref="IntervalLiteral(string, IntervalField)"/>. <c>SECOND</c> is
    /// never a leading field of a range — use <see cref="ToSecond(int?)"/> for
    /// a range's trailing field.
    /// </summary>
    /// <returns>An <see cref="IntervalField"/> emitting <c>SECOND</c>.</returns>
    /// <remarks>
    /// No precision parameter: Oracle's standalone <c>SECOND</c> takes a
    /// <c>(leading, fractional)</c> pair, which has no spelling here. For the
    /// single-value form, <see cref="ToSecond(int?)"/> emits <c>SECOND(n)</c>.
    /// </remarks>
    public static IntervalField Second() => new(DateTimePart.Second, null);

    /// <summary>
    /// Begins a <c>SELECT</c> statement projecting <paramref name="selectItems"/>.
    /// Continue with <c>.From(...)</c> and the remaining clauses. Each item is a
    /// column, expression, or <c>expr.As("alias")</c>.
    /// </summary>
    /// <param name="selectItems">The columns or expressions to project.</param>
    /// <returns>A select builder positioned for <c>.From(...)</c>.</returns>
    public static ISelectBuilderSelect Select(params object[] selectItems) =>
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
    /// <param name="distinctOn">PostgreSQL's <c>DISTINCT ON (...)</c> prefix (<see cref="DistinctOn(object[])"/>), emitting <c>SELECT DISTINCT ON (...)</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        DistinctOnKeyword distinctOn,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithDistinct.Parse(
                distinctOn,
                selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints (<see cref="Hints(string)"/>), emitted after <c>SELECT</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(SqlHints hints, params object[] selectItems) =>
        new SelectBuilder(SelectClauseWithHints.Parse(hints, selectItems));

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

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints (<see cref="Hints(string)"/>), emitted after <c>SELECT</c>.</param>
    /// <param name="distinctOn">PostgreSQL's <c>DISTINCT ON (...)</c> prefix (<see cref="DistinctOn(object[])"/>), emitting <c>SELECT ... DISTINCT ON (...)</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctOnKeyword distinctOn,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithOptions.Parse(
                hints,
                distinctOn,
                selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="top">SQL Server's <c>TOP (n)</c> prefix (<see cref="Top(int)"/>), emitting <c>SELECT TOP (n)</c>.</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(TopClause top, params object[] selectItems) =>
        new SelectBuilder(SelectClauseWithTop.Parse(top, selectItems));

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Distinct"/>), emitting <c>SELECT DISTINCT TOP (n)</c>.</param>
    /// <param name="top">SQL Server's <c>TOP (n)</c> prefix (<see cref="Top(int)"/>).</param>
    /// <param name="selectItems">The columns or expressions to project.</param>
    public static ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        TopClause top,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithDistinctTop.Parse(
                distinct,
                top,
                selectItems));

    /// <summary>
    /// Wraps a <c>GROUP_CONCAT</c> separator in MySQL's <c>SEPARATOR</c> keyword
    /// form.
    /// </summary>
    /// <param name="separator">The separator string placed between concatenated values.</param>
    /// <returns>A <c>SEPARATOR</c> clause for <c>GROUP_CONCAT</c>.</returns>
    /// <remarks>
    /// Distinct from SQLite's positional separator argument.
    /// </remarks>
    public static SeparatorClause Separator(string separator) =>
        new(separator);

    /// <summary>
    /// A reference to a sequence using the Oracle dotted syntax,
    /// e.g. <c>name.NEXTVAL</c> / <c>name.CURRVAL</c> via <c>.Nextval</c> / <c>.Currval</c>.
    /// </summary>
    /// <param name="name">The name of the sequence to reference.</param>
    /// <returns>A sequence reference exposing <c>.Nextval</c> / <c>.Currval</c>.</returns>
    /// <remarks>
    /// Dialect-specific (Oracle). For PostgreSQL use <see cref="Nextval(string)"/> /
    /// <see cref="Currval(string)"/>; for SQL Server use <see cref="NextValueFor(string)"/>.
    /// </remarks>
    public static DbSequence Sequence(string name) => new(name);

    /// <summary>
    /// The <c>SIGN(expr)</c> function: the sign of <paramref name="expr"/>
    /// (-1, 0, or 1).
    /// </summary>
    /// <param name="expr">The numeric expression whose sign is taken.</param>
    /// <returns>A <c>SIGN</c> function expression.</returns>
    public static SignFunction Sign(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>SKIP LOCKED</c> behavior for a <c>FOR UPDATE</c> clause: skip rows that
    /// are already locked instead of blocking on them.
    /// </summary>
    /// <remarks>MySQL 8.0+, Oracle, and PostgreSQL syntax.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SkipLockedBehavior SkipLocked => new();

    /// <summary>
    /// The <c>SOME (subquery)</c> quantified comparison operator: equivalent to
    /// <see cref="Any(ISubquery)"/> — the comparison must hold for at least one row.
    /// Use with a comparison operator — e.g. <c>col = Some(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>SOME (SELECT ...)</c>.</returns>
    /// <remarks>SQLite's grammar has no quantified comparisons; the other dialects accept it.</remarks>
    public static QuantifiedSubquery Some(ISubquery subquery) =>
        new(Keywords.Some, subquery);

    /// <summary>
    /// The <c>SOME (array)</c> quantified comparison operator: equivalent to
    /// <see cref="Any(SqlExpression)"/> — the comparison must hold for at least
    /// one element of the array expression (PostgreSQL).
    /// Use with a comparison operator — e.g. <c>col == Some(BindArray(values))</c>.
    /// </summary>
    /// <param name="array">The array expression — a <see cref="BindArrayValue"/>,
    /// an <c>ARRAY[...]</c> constructor, or an array-typed column.</param>
    /// <returns>A quantified expression emitting <c>SOME (array)</c>.</returns>
    public static QuantifiedExpression Some(SqlExpression array) =>
        new(Keywords.Some, array);

    /// <summary>
    /// The <c>SQRT(expr)</c> function: the square root of <paramref name="expr"/>.
    /// </summary>
    /// <param name="expr">The numeric expression whose square root is taken.</param>
    /// <returns>A <c>SQRT</c> function expression.</returns>
    public static SqrtFunction Sqrt(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STDDEV(<paramref name="expr"/>)</c> aggregate function.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="StddevFunction"/> emitting <c>STDDEV(expr)</c>.</returns>
    /// <remarks>
    /// MySQL, Oracle, PostgreSQL — but not the same statistic on all three:
    /// MySQL's <c>STDDEV</c> is the population standard deviation, Oracle's and
    /// PostgreSQL's is the sample standard deviation. For a value that keeps its
    /// meaning across dialects, use <see cref="StddevPop(object)"/> or
    /// <see cref="StddevSamp(object)"/> instead.
    /// </remarks>
    public static StddevFunction Stddev(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STDDEV_POP(<paramref name="expr"/>)</c> aggregate function: the
    /// population standard deviation of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="StddevPopFunction"/> emitting <c>STDDEV_POP(expr)</c>.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL. SQL Server spells this
    /// <see cref="Stdevp(object)"/>.</remarks>
    public static StddevPopFunction StddevPop(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STDDEV_SAMP(<paramref name="expr"/>)</c> aggregate function: the
    /// sample standard deviation of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="StddevSampFunction"/> emitting <c>STDDEV_SAMP(expr)</c>.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL. SQL Server spells this
    /// <see cref="Stdev(object)"/>.</remarks>
    public static StddevSampFunction StddevSamp(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STDEV(<paramref name="expr"/>)</c> aggregate function: the sample
    /// standard deviation of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="StdevFunction"/> emitting <c>STDEV(expr)</c>.</returns>
    /// <remarks>SQL Server syntax — no double-D. MySQL, Oracle, and PostgreSQL
    /// spell this <see cref="StddevSamp(object)"/>.</remarks>
    public static StdevFunction Stdev(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STDEVP(<paramref name="expr"/>)</c> aggregate function: the
    /// population standard deviation of <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The numeric expression to aggregate.</param>
    /// <returns>A <see cref="StdevpFunction"/> emitting <c>STDEVP(expr)</c>.</returns>
    /// <remarks>SQL Server syntax — no double-D. MySQL, Oracle, and PostgreSQL
    /// spell this <see cref="StddevPop(object)"/>.</remarks>
    public static StdevpFunction Stdevp(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>STRFTIME(<paramref name="format"/>, <paramref name="timevalue"/>, ...)</c>
    /// function: <paramref name="timevalue"/> shifted/adjusted by each modifier in
    /// order, then rendered as a string per <paramref name="format"/>'s
    /// strftime-style specifiers (<c>%Y</c>, <c>%m</c>, <c>%d</c>, ...).
    /// </summary>
    /// <param name="format">The strftime-style format string.</param>
    /// <param name="timevalue">The time value to format.</param>
    /// <param name="modifiers">Modifier strings applied in order; optional.</param>
    /// <returns>The <c>STRFTIME</c> function expression.</returns>
    /// <remarks>
    /// SQLite syntax — see <see cref="Date(object, object, object[])"/> for why
    /// modifiers are plain strings.
    /// </remarks>
    public static StrftimeFunction Strftime(object format, object timevalue, params object[] modifiers) =>
        new(ResolveVariadic(format, timevalue, modifiers));

    /// <summary>
    /// The <c>STRING_AGG(expr, separator)</c> string aggregate: concatenates
    /// <paramref name="expr"/> across the group, separated by
    /// <paramref name="separator"/>.
    /// </summary>
    /// <param name="expr">The expression aggregated across the group.</param>
    /// <param name="separator">The string placed between concatenated values. Emitted as an inline string literal, since SQL Server requires the separator to be a literal.</param>
    /// <returns>A <c>STRING_AGG</c> aggregate expression.</returns>
    /// <remarks>
    /// PostgreSQL, SQLite (3.44+), and SQL Server. Order per dialect: pass
    /// <c>OrderBy(...)</c> as an argument for PostgreSQL / SQLite, or chain
    /// <c>.WithinGroup(OrderBy(...))</c> for SQL Server's <c>WITHIN GROUP</c> form.
    /// </remarks>
    public static StringAggFunction StringAgg(object expr, string separator) =>
        new(Resolve(expr), separator);

    /// <inheritdoc cref="StringAgg(object, string)"/>
    /// <param name="expr">The expression aggregated across the group.</param>
    /// <param name="separator">The string placed between concatenated values. Emitted as an inline string literal, since SQL Server requires the separator to be a literal.</param>
    /// <param name="orderByClause">PostgreSQL's inline ordering, emitted inside the call (<c>STRING_AGG(expr, separator ORDER BY ...)</c>).</param>
    public static StringAggFunction StringAgg(
        object expr,
        string separator,
        OrderByClause orderByClause) => new(Resolve(expr), separator, orderByClause);

    /// <summary>
    /// The <c>STRPOS(<paramref name="source"/>, <paramref name="substring"/>)</c>
    /// function: the 1-based index of the first occurrence of
    /// <paramref name="substring"/> in <paramref name="source"/>, or 0 if absent.
    /// </summary>
    /// <param name="source">The string searched.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <returns>A <see cref="StrposFunction"/> emitting <c>STRPOS(source, substring)</c>.</returns>
    /// <remarks>PostgreSQL syntax. For the keyword form (MySQL, PostgreSQL) with the
    /// reverse argument order, see <see cref="Position(object, object)"/>.</remarks>
    public static StrposFunction Strpos(object source, object substring) =>
        new(Resolve(source), Resolve(substring));

    /// <summary>
    /// The <c>SUBSTR(source, position)</c> function: the substring of
    /// <paramref name="source"/> from the 1-based <paramref name="position"/> to the
    /// end.
    /// </summary>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position.</param>
    /// <returns>A <c>SUBSTR</c> function expression.</returns>
    /// <remarks>Not supported by SQL Server — use <see cref="Substring(object, object, object)"/>
    /// there instead.</remarks>
    public static SubstrFunction Substr(object source, object position) =>
        new(Resolve(source), Resolve(position));

    /// <inheritdoc cref="Substr(object, object)"/>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position.</param>
    /// <param name="length">The number of characters to take.</param>
    public static SubstrFunction Substr(object source, object position, object length) =>
        new(Resolve(source), Resolve(position), Resolve(length));

    /// <summary>
    /// The <c>SUBSTRB(source, position)</c> function: like <see cref="Substr(object, object)"/>
    /// but with <paramref name="position"/> measured in bytes.
    /// </summary>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position, in bytes.</param>
    /// <returns>A <c>SUBSTRB</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static SubstrbFunction Substrb(object source, object position) =>
        new(Resolve(source), Resolve(position));

    /// <inheritdoc cref="Substrb(object, object)"/>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position, in bytes.</param>
    /// <param name="length">The number of bytes to take.</param>
    public static SubstrbFunction Substrb(object source, object position, object length) =>
        new(Resolve(source), Resolve(position), Resolve(length));

    /// <summary>
    /// The <c>SUBSTRING(source, position, length)</c> function: the substring of
    /// <paramref name="source"/> from the 1-based <paramref name="position"/>, taking
    /// <paramref name="length"/> characters.
    /// </summary>
    /// <param name="source">The string to slice.</param>
    /// <param name="position">The 1-based start position.</param>
    /// <param name="length">The number of characters to take.</param>
    /// <returns>A <see cref="SubstringFunction"/> emitting <c>SUBSTRING(source, position, length)</c>.</returns>
    /// <remarks>MySQL, PostgreSQL, SQLite (3.34+), and SQL Server syntax — SQL Server's
    /// only substring spelling, since it has no <c>SUBSTR</c>. Use
    /// <see cref="Substr(object, object, object)"/> where this construct is unavailable.
    /// Unlike <c>Substr</c>, <paramref name="length"/> is required: SQL Server's grammar
    /// has no 2-argument form.</remarks>
    public static SubstringFunction Substring(object source, object position, object length) =>
        new(Resolve(source), Resolve(position), Resolve(length));

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
    /// The <c>SYSDATE</c> function.
    /// </summary>
    /// <remarks>Oracle syntax. For the standard current date/time use
    /// <see cref="CurrentTimestamp"/> or <see cref="CurrentDate"/>.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction Sysdate => new();

    /// <summary>
    /// The <c>SYSTIMESTAMP</c> function.
    /// </summary>
    /// <remarks>Oracle syntax. For the standard current timestamp use
    /// <see cref="CurrentTimestamp"/>.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction Systimestamp => new();
}
