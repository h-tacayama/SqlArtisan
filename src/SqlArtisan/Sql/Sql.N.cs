using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>(leftVector &lt;#&gt; rightVector)</c> negative inner product
    /// operator between two vectors — smaller means more similar (Oracle 23ai+,
    /// PostgreSQL). On PostgreSQL it requires the pgvector extension.
    /// </summary>
    /// <param name="leftVector">The first vector.</param>
    /// <param name="rightVector">The second vector.</param>
    /// <returns>A <c>&lt;#&gt;</c> operator expression.</returns>
    public static NegativeInnerProductOperator NegativeInnerProduct(
        object leftVector,
        object rightVector) =>
        new(Resolve(leftVector), Resolve(rightVector));

    /// <summary>
    /// The <c>NEXTVAL('sequenceName')</c> function: the next value of the named
    /// sequence.
    /// </summary>
    /// <param name="sequenceName">The name of the sequence to advance.</param>
    /// <returns>A <c>NEXTVAL</c> function expression.</returns>
    /// <remarks>
    /// Dialect-specific (PostgreSQL). For Oracle use <see cref="Sequence(string)"/>
    /// with <c>.Nextval</c>; for SQL Server use <see cref="NextValueFor(string)"/>.
    /// </remarks>
    public static NextvalFunction Nextval(string sequenceName) =>
        new(sequenceName);

    /// <summary>
    /// The <c>NEXT VALUE FOR sequenceName</c> expression: the next value of the
    /// named sequence.
    /// </summary>
    /// <param name="sequenceName">The name of the sequence to advance.</param>
    /// <returns>A <c>NEXT VALUE FOR</c> expression.</returns>
    /// <remarks>
    /// Dialect-specific (SQL Server). For Oracle use <see cref="Sequence(string)"/>
    /// with <c>.Nextval</c>; for PostgreSQL use <see cref="Nextval(string)"/>.
    /// </remarks>
    public static NextValueForFunction NextValueFor(string sequenceName) =>
        new(sequenceName);

    /// <summary>
    /// The <c>NOT (condition)</c> predicate: negates <paramref name="condition"/>.
    /// </summary>
    /// <param name="condition">The condition to negate.</param>
    /// <returns>A negated condition.</returns>
    public static NotCondition Not(SqlCondition condition) => new(condition);

    /// <summary>
    /// The <c>NOT EXISTS (subquery)</c> predicate: true when
    /// <paramref name="subquery"/> returns no rows.
    /// </summary>
    /// <param name="subquery">The subquery to test for emptiness.</param>
    /// <returns>A <c>NOT EXISTS</c> condition.</returns>
    public static NotExistsCondition NotExists(ISubquery subquery) =>
        new(subquery);

    /// <summary>
    /// The <c>NOWAIT</c> lock behavior for a <c>FOR UPDATE</c> clause: fail
    /// immediately rather than block when a row is already locked.
    /// </summary>
    /// <remarks>MySQL (8.0+), Oracle, and PostgreSQL syntax.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NowaitBehavior Nowait => new();

    /// <summary>
    /// The <c>NTH_VALUE(expr, n)</c> analytic function: the value of
    /// <paramref name="expr"/> from the <paramref name="n"/>th row of the window
    /// frame.
    /// </summary>
    /// <param name="expr">The expression whose value is taken.</param>
    /// <param name="n">The 1-based position within the window frame; positive.</param>
    /// <returns>An <c>NTH_VALUE</c> analytic function expression.</returns>
    /// <remarks>Not supported by SQL Server.</remarks>
    public static AnalyticNthValueFunction NthValue(object expr, int n) =>
        new(Resolve(expr), n);

    /// <summary>
    /// The <c>NTILE(buckets)</c> analytic function: distributes the ordered rows
    /// of each window partition into <paramref name="buckets"/> ranked groups.
    /// </summary>
    /// <param name="buckets">The number of ranked groups to distribute rows into; positive.</param>
    /// <returns>An <c>NTILE</c> analytic function expression.</returns>
    public static AnalyticNtileFunction Ntile(int buckets) => new(buckets);

    /// <summary>
    /// The SQL <c>NULL</c> literal.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NullExpression Null => new();

    /// <summary>
    /// The <c>NULLIF(expr1, expr2)</c> function: returns <c>NULL</c> when the two
    /// expressions are equal, otherwise <paramref name="expr1"/>.
    /// </summary>
    /// <param name="expr1">The expression returned when the two differ.</param>
    /// <param name="expr2">The expression compared against <paramref name="expr1"/>.</param>
    /// <returns>A <c>NULLIF</c> function expression.</returns>
    public static NullifFunction Nullif(object expr1, object expr2) =>
        new(Resolve(expr1), Resolve(expr2));

    /// <summary>
    /// The <c>NUMTODSINTERVAL(<paramref name="n"/>, 'unit')</c> function:
    /// converts <paramref name="n"/> into an <c>INTERVAL DAY TO SECOND</c>
    /// value, in <paramref name="unit"/>.
    /// </summary>
    /// <param name="n">The number of <paramref name="unit"/>s.</param>
    /// <param name="unit">The interval unit — <see cref="DateTimePart.Day"/>,
    /// <see cref="DateTimePart.Hour"/>, <see cref="DateTimePart.Minute"/>, or
    /// <see cref="DateTimePart.Second"/>.</param>
    /// <returns>A <c>NUMTODSINTERVAL</c> function expression.</returns>
    /// <exception cref="ArgumentException"><paramref name="unit"/> is none of
    /// the four values Oracle's own function accepts.</exception>
    /// <remarks>Oracle syntax — the bindable-quantity counterpart to
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>'s
    /// <c>DAY TO SECOND</c> literal.</remarks>
    public static NumtodsintervalFunction Numtodsinterval(object n, DateTimePart unit) =>
        new(Resolve(n), unit);

    /// <summary>
    /// The <c>NUMTOYMINTERVAL(<paramref name="n"/>, 'unit')</c> function:
    /// converts <paramref name="n"/> into an <c>INTERVAL YEAR TO MONTH</c>
    /// value, in <paramref name="unit"/>.
    /// </summary>
    /// <param name="n">The number of <paramref name="unit"/>s.</param>
    /// <param name="unit">The interval unit — <see cref="DateTimePart.Year"/>
    /// or <see cref="DateTimePart.Month"/>.</param>
    /// <returns>A <c>NUMTOYMINTERVAL</c> function expression.</returns>
    /// <exception cref="ArgumentException"><paramref name="unit"/> is neither
    /// <see cref="DateTimePart.Year"/> nor <see cref="DateTimePart.Month"/>.</exception>
    /// <remarks>Oracle syntax — the bindable-quantity counterpart to
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>'s
    /// <c>YEAR TO MONTH</c> literal.</remarks>
    public static NumtoymintervalFunction Numtoyminterval(object n, DateTimePart unit) =>
        new(Resolve(n), unit);

    /// <summary>
    /// The <c>NVL(expr1, expr2)</c> function: returns <paramref name="expr1"/> when
    /// it is not <see langword="null"/>, otherwise <paramref name="expr2"/>.
    /// </summary>
    /// <param name="expr1">The expression returned when it is not <see langword="null"/>.</param>
    /// <param name="expr2">The fallback returned when <paramref name="expr1"/> is <see langword="null"/>.</param>
    /// <returns>An <c>NVL</c> function expression.</returns>
    /// <remarks>Oracle syntax. For the standard equivalent use
    /// <see cref="Coalesce(object, object, object[])"/>.</remarks>
    public static NvlFunction Nvl(object expr1, object expr2) =>
        new(Resolve(expr1), Resolve(expr2));
}
