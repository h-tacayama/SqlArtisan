using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>(leftVector &lt;+&gt; rightVector)</c> L1 (taxicab) distance operator
    /// between two vectors (PostgreSQL). Requires the pgvector extension (0.7.0+).
    /// </summary>
    /// <param name="leftVector">The first vector.</param>
    /// <param name="rightVector">The second vector.</param>
    /// <returns>A <c>&lt;+&gt;</c> operator expression.</returns>
    /// <remarks>PostgreSQL syntax; Oracle has no L1 shorthand.</remarks>
    public static SqlExpression L1Distance(object leftVector, object rightVector) =>
        new L1DistanceOperator(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>(leftVector &lt;-&gt; rightVector)</c> L2 (Euclidean) distance
    /// operator between two vectors (Oracle 23ai+, PostgreSQL). On PostgreSQL it
    /// requires the pgvector extension.
    /// </summary>
    /// <param name="leftVector">The first vector.</param>
    /// <param name="rightVector">The second vector.</param>
    /// <returns>A <c>&lt;-&gt;</c> operator expression.</returns>
    /// <remarks>Oracle (23ai+) and PostgreSQL syntax.</remarks>
    public static SqlExpression L2Distance(object leftVector, object rightVector) =>
        new L2DistanceOperator(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>LAG(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position before the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>A window function emitting <c>LAG(expr)</c>.</returns>
    public static AnalyticFunction Lag(object expr) =>
        new AnalyticLagFunction(Resolve(expr));

    /// <summary>
    /// The <c>LAG(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <returns>A window function emitting <c>LAG(expr, offset)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal.</remarks>
    public static AnalyticFunction Lag(object expr, int offset) =>
        new AnalyticLagFunction(Resolve(expr), offset);

    /// <summary>
    /// The <c>LAG(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>A window function emitting <c>LAG(expr, offset, default)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal; the default value is
    /// parameterized.</remarks>
    public static AnalyticFunction Lag(object expr, int offset, object defaultValue) =>
        new AnalyticLagFunction(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LAST_DAY(<paramref name="date"/>)</c> function: the date
    /// of the last day of the month containing <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date whose month's last day is returned.</param>
    /// <returns>The LAST_DAY construct.</returns>
    /// <remarks>MySQL and Oracle syntax.</remarks>
    public static SqlExpression LastDay(object date) =>
        new LastDayFunction(Resolve(date));

    /// <summary>
    /// The <c>LAST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the last row of the window frame.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>A window function emitting <c>LAST_VALUE(expr)</c>.</returns>
    /// <remarks>Frame-sensitive, and the frame without an explicit one is the engine's, so
    /// an explicit frame (e.g. <c>ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED
    /// FOLLOWING</c>) is usually intended.</remarks>
    public static ValueAnalyticFunction LastValue(object expr) =>
        new AnalyticLastValueFunction(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position after the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>A window function emitting <c>LEAD(expr)</c>.</returns>
    public static AnalyticFunction Lead(object expr) =>
        new AnalyticLeadFunction(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <returns>A window function emitting <c>LEAD(expr, offset)</c>.</returns>
    /// <inheritdoc cref="Lag(object, int)" path="/remarks"/>
    public static AnalyticFunction Lead(object expr, int offset) =>
        new AnalyticLeadFunction(Resolve(expr), offset);

    /// <summary>
    /// The <c>LEAD(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>A window function emitting <c>LEAD(expr, offset, default)</c>.</returns>
    /// <inheritdoc cref="Lag(object, int, object)" path="/remarks"/>
    public static AnalyticFunction Lead(object expr, int offset, object defaultValue) =>
        new AnalyticLeadFunction(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LEAST(a, b, ...)</c> function: the smallest of its
    /// <paramref name="expressions"/>.
    /// </summary>
    /// <param name="expressions">The values to compare.</param>
    /// <returns>The LEAST construct.</returns>
    /// <remarks>Not supported by SQLite — its multi-argument
    /// <c>MIN(a, b, ...)</c> is the equivalent; SQL Server (2022+).</remarks>
    public static SqlExpression Least(params object[] expressions) =>
        new LeastFunction(Resolve(expressions));

    /// <summary>
    /// The <c>LEFT(<paramref name="source"/>, <paramref name="length"/>)</c> function:
    /// the leftmost <paramref name="length"/> characters of <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to take characters from.</param>
    /// <param name="length">The number of characters to take.</param>
    /// <returns>The LEFT construct.</returns>
    /// <remarks>MySQL, PostgreSQL, and SQL Server syntax. Oracle and SQLite have no
    /// <c>LEFT</c> — slice with <see cref="Substr(object, object, object)"/> there.</remarks>
    public static SqlExpression Left(object source, object length) =>
        new LeftFunction(Resolve(source), Resolve(length));

    /// <summary>
    /// The <c>LENGTH(<paramref name="source"/>)</c> function: the number of
    /// characters in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string whose length is measured.</param>
    /// <returns>The LENGTH construct.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, and SQLite syntax. Not available on
    /// SQL Server.</remarks>
    public static SqlExpression Length(object source) =>
        new LengthFunction(Resolve(source));

    /// <summary>
    /// The <c>LENGTHB(<paramref name="source"/>)</c> function: the
    /// length of <paramref name="source"/> in bytes.
    /// </summary>
    /// <param name="source">The string whose byte length is measured.</param>
    /// <returns>The LENGTHB construct.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static SqlExpression Lengthb(object source) =>
        new LengthbFunction(Resolve(source));

    /// <summary>
    /// The <c>LISTAGG(expr, separator)</c> string aggregate. Complete
    /// it with <c>.WithinGroup(OrderBy(...))</c> before it can be used as an
    /// expression.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separator">The separator placed between values.</param>
    /// <returns>A <see cref="ListaggFunction"/> emitting
    /// <c>LISTAGG(expr, separator)</c>.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ListaggFunction Listagg(object expr, object separator) =>
        new(Resolve(expr), Resolve(separator));

    /// <summary>
    /// The <c>LN(<paramref name="expr"/>)</c> function: the natural (base e)
    /// logarithm of <paramref name="expr"/>.
    /// </summary>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>An <c>LN</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite (3.35+) syntax. SQL Server has
    /// no <c>LN</c> — its single-argument <see cref="Log(object)"/> is the
    /// natural logarithm.</remarks>
    public static SqlExpression Ln(object expr) =>
        new LnFunction(Resolve(expr));

    /// <summary>
    /// The <c>LOG(<paramref name="expr"/>)</c> function: the logarithm of
    /// <paramref name="expr"/> in the target dialect's own default base.
    /// </summary>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG</c> function expression.</returns>
    /// <remarks>The base is dialect-defined, and the difference is silent: 10 on
    /// PostgreSQL and SQLite (3.35+), e on MySQL and SQL Server. Oracle has no
    /// single-argument form. Where the base must not depend on the target, use
    /// <see cref="Ln(object)"/>, <see cref="Log10(object)"/>, or
    /// <see cref="Log(object, object)"/>.</remarks>
    public static SqlExpression Log(object expr) =>
        new LogFunction(Resolve(expr));

    /// <summary>
    /// The <c>LOG(<paramref name="base"/>, <paramref name="expr"/>)</c> function:
    /// the logarithm of <paramref name="expr"/> to base <paramref name="base"/>.
    /// </summary>
    /// <param name="base">The base of the logarithm.</param>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite (3.35+) syntax — all four read the base
    /// first; PostgreSQL takes <c>numeric</c> only. SQL Server reads <c>LOG(value, base)</c>:
    /// swap the arguments there, and suppress the <c>SQLA0100</c> it still draws.</remarks>
    public static SqlExpression Log(object @base, object expr) =>
        new LogFunction(Resolve(@base), Resolve(expr));

    /// <summary>
    /// The <c>LOG10(<paramref name="expr"/>)</c> function: the base-10 logarithm
    /// of <paramref name="expr"/>.
    /// </summary>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG10</c> function expression.</returns>
    /// <remarks>MySQL, PostgreSQL (12+), SQLite (3.35+), SQL Server syntax.
    /// Oracle has no <c>LOG10</c> — spell it <c>Log(10, expr)</c> there.</remarks>
    public static SqlExpression Log10(object expr) =>
        new Log10Function(Resolve(expr));

    /// <summary>
    /// The <c>LOWER(<paramref name="source"/>)</c> function: lowercases
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to lowercase.</param>
    /// <returns>The LOWER construct.</returns>
    public static SqlExpression Lower(object source) =>
        new LowerFunction(Resolve(source));

    /// <summary>
    /// The <c>LPAD(<paramref name="source"/>, <paramref name="length"/>)</c>
    /// function: left-pads <paramref name="source"/> with spaces to
    /// <paramref name="length"/> characters (truncating if longer).
    /// </summary>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <returns>The LPAD construct.</returns>
    /// <remarks>Oracle and PostgreSQL syntax.</remarks>
    public static SqlExpression Lpad(object source, object length) =>
        new LpadFunction(Resolve(source), Resolve(length));

    /// <summary>
    /// The <c>LPAD(<paramref name="source"/>, <paramref name="length"/>,
    /// <paramref name="padding"/>)</c> function: left-pads <paramref name="source"/> with
    /// <paramref name="padding"/> to <paramref name="length"/> characters (truncating if longer).
    /// </summary>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <param name="padding">The string to pad with instead of spaces.</param>
    /// <returns>The LPAD construct.</returns>
    /// <remarks>MySQL, Oracle, and PostgreSQL syntax.</remarks>
    public static SqlExpression Lpad(object source, object length, object padding) =>
        new LpadFunction(Resolve(source), Resolve(length), Resolve(padding));

    /// <summary>
    /// The <c>LTRIM(<paramref name="source"/>)</c> function: removes leading
    /// whitespace from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>The LTRIM construct.</returns>
    public static SqlExpression Ltrim(object source) =>
        new LtrimFunction(Resolve(source));

    /// <summary>
    /// The <c>LTRIM(<paramref name="source"/>, <paramref name="trimChars"/>)</c>
    /// function: removes any of <paramref name="trimChars"/> from the left of
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChars">The set of characters to strip from the left.</param>
    /// <returns>The LTRIM construct.</returns>
    /// <remarks>Oracle, PostgreSQL, SQLite, and SQL Server (2022+) syntax; SQL
    /// Server also requires database compatibility level 160, the default for
    /// new databases.</remarks>
    public static SqlExpression Ltrim(object source, object trimChars) =>
        new LtrimFunction(Resolve(source), Resolve(trimChars));
}
