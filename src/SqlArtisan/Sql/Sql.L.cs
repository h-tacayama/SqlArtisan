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
    public static L1DistanceOperator L1Distance(object leftVector, object rightVector) =>
        new(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>(leftVector &lt;-&gt; rightVector)</c> L2 (Euclidean) distance
    /// operator between two vectors (Oracle 23ai+, PostgreSQL). On PostgreSQL it
    /// requires the pgvector extension.
    /// </summary>
    /// <param name="leftVector">The first vector.</param>
    /// <param name="rightVector">The second vector.</param>
    /// <returns>A <c>&lt;-&gt;</c> operator expression.</returns>
    public static L2DistanceOperator L2Distance(object leftVector, object rightVector) =>
        new(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>LAG(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position before the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr)</c>.</returns>
    public static AnalyticLagFunction Lag(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LAG(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr, offset)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal.</remarks>
    public static AnalyticLagFunction Lag(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LAG(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr, offset, default)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal; the default value is
    /// parameterized.</remarks>
    public static AnalyticLagFunction Lag(object expr, int offset, object defaultValue) =>
        new(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LAST_DAY(<paramref name="date"/>)</c> function: the date
    /// of the last day of the month containing <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date whose month's last day is returned.</param>
    /// <returns>The LAST_DAY construct.</returns>
    /// <remarks>MySQL and Oracle syntax.</remarks>
    public static LastDayFunction LastDay(object date) =>
        new(Resolve(date));

    /// <summary>
    /// The <c>LAST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the last row of the window frame.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLastValueFunction"/> emitting <c>LAST_VALUE(expr)</c>.</returns>
    /// <remarks>The default frame ends at the current row, so an explicit frame
    /// (e.g. <c>ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING</c>) is
    /// usually intended.</remarks>
    public static AnalyticLastValueFunction LastValue(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position after the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr)</c>.</returns>
    public static AnalyticLeadFunction Lead(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr, offset)</c>.</returns>
    /// <inheritdoc cref="Lag(object, int)" path="/remarks"/>
    public static AnalyticLeadFunction Lead(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LEAD(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr, offset, default)</c>.</returns>
    /// <inheritdoc cref="Lag(object, int, object)" path="/remarks"/>
    public static AnalyticLeadFunction Lead(object expr, int offset, object defaultValue) =>
        new(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LEAST(a, b, ...)</c> function: the smallest of its
    /// <paramref name="expressions"/>.
    /// </summary>
    /// <param name="expressions">The values to compare.</param>
    /// <returns>The LEAST construct.</returns>
    /// <remarks>SQLite has no <c>LEAST</c> — its multi-argument
    /// <c>MIN(a, b, ...)</c> is the equivalent; SQL Server 2022+.</remarks>
    public static LeastFunction Least(params object[] expressions) =>
        new(Resolve(expressions));

    /// <summary>
    /// The <c>LENGTH(<paramref name="source"/>)</c> function: the number of
    /// characters in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string whose length is measured.</param>
    /// <returns>The LENGTH construct.</returns>
    public static LengthFunction Length(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LENGTHB(<paramref name="source"/>)</c> function: the
    /// length of <paramref name="source"/> in bytes.
    /// </summary>
    /// <param name="source">The string whose byte length is measured.</param>
    /// <returns>The LENGTHB construct.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static LengthbFunction Lengthb(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LISTAGG(expr, separator)</c> string aggregate. Complete
    /// it with <c>.WithinGroup(OrderBy(...))</c> to supply Oracle's mandatory
    /// ordering.
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
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite syntax. SQL Server has no
    /// <c>LN</c> — its single-argument <see cref="Log(object)"/> is the natural
    /// logarithm.</remarks>
    public static LnFunction Ln(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LOG(<paramref name="expr"/>)</c> function: the logarithm of
    /// <paramref name="expr"/> in the target dialect's own default base.
    /// </summary>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG</c> function expression.</returns>
    /// <remarks>The base is dialect-defined, and the difference is silent: 10 on
    /// PostgreSQL and SQLite, e on MySQL and SQL Server. Oracle has no
    /// single-argument form. Where the base must not depend on the target, use
    /// <see cref="Ln(object)"/>, <see cref="Log10(object)"/>, or
    /// <see cref="Log(object, object)"/>.</remarks>
    public static LogFunction Log(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LOG(<paramref name="base"/>, <paramref name="expr"/>)</c> function:
    /// the logarithm of <paramref name="expr"/> to base <paramref name="base"/>.
    /// </summary>
    /// <param name="base">The base of the logarithm.</param>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite syntax — all four read the base
    /// first. SQL Server takes the two arguments in the opposite order
    /// (<c>LOG(value, base)</c>), so the same text runs there and returns a
    /// different number; spell that dialect's call <see cref="Log(object)"/> or
    /// <see cref="Log10(object)"/> instead. On PostgreSQL this form is defined for
    /// <c>numeric</c> only, not <c>double precision</c>.</remarks>
    public static LogFunction Log(object @base, object expr) =>
        new(Resolve(@base), Resolve(expr));

    /// <summary>
    /// The <c>LOG10(<paramref name="expr"/>)</c> function: the base-10 logarithm
    /// of <paramref name="expr"/>.
    /// </summary>
    /// <param name="expr">The value whose logarithm is taken.</param>
    /// <returns>A <c>LOG10</c> function expression.</returns>
    /// <remarks>MySQL, PostgreSQL (12+), SQLite, SQL Server syntax. Oracle has no
    /// <c>LOG10</c> — spell it <c>Log(10, expr)</c> there.</remarks>
    public static Log10Function Log10(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LOWER(<paramref name="source"/>)</c> function: lowercases
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to lowercase.</param>
    /// <returns>The LOWER construct.</returns>
    public static LowerFunction Lower(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LPAD(<paramref name="source"/>, <paramref name="length"/>)</c>
    /// function: left-pads <paramref name="source"/> with spaces to
    /// <paramref name="length"/> characters (truncating if longer).
    /// </summary>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <returns>The LPAD construct.</returns>
    /// <remarks>Oracle and PostgreSQL syntax.</remarks>
    public static LpadFunction Lpad(object source, object length) =>
        new(Resolve(source), Resolve(length));

    /// <inheritdoc cref="Lpad(object, object)"/>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <param name="padding">The string to pad with instead of spaces.</param>
    /// <remarks>MySQL, Oracle, and PostgreSQL syntax.</remarks>
    public static LpadFunction Lpad(object source, object length, object padding) =>
        new(Resolve(source), Resolve(length), Resolve(padding));

    /// <summary>
    /// The <c>LTRIM(<paramref name="source"/>)</c> function: removes leading
    /// whitespace from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>The LTRIM construct.</returns>
    public static LtrimFunction Ltrim(object source) =>
        new(Resolve(source));

    /// <inheritdoc cref="Ltrim(object)"/>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChars">The set of characters to strip from the left.</param>
    /// <remarks>Oracle, PostgreSQL, SQLite, and SQL Server (2022+) syntax; SQL
    /// Server also requires database compatibility level 160, the default for
    /// new databases.</remarks>
    public static LtrimFunction Ltrim(object source, object trimChars) =>
        new(Resolve(source), Resolve(trimChars));
}
