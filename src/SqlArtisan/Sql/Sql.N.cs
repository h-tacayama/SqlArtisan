using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Gets the next value of a sequence using the PostgreSQL syntax
    /// <c>NEXTVAL('sequenceName')</c>.
    /// </summary>
    /// <remarks>
    /// Dialect-specific (PostgreSQL). For Oracle use <see cref="Sequence(string)"/>
    /// with <c>.Nextval</c>; for SQL Server use <see cref="NextValueFor(string)"/>.
    /// </remarks>
    public static NextvalFunction Nextval(string sequenceName) =>
        new(sequenceName);

    /// <summary>
    /// Gets the next value of a sequence using the SQL Server syntax
    /// <c>NEXT VALUE FOR sequenceName</c>.
    /// </summary>
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
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NowaitBehavior Nowait => new();

    /// <summary>
    /// The <c>NTH_VALUE(expr, n)</c> analytic function: the value of
    /// <paramref name="expr"/> from the <paramref name="n"/>th row of the window
    /// frame. The position is emitted as an integer literal (not a bind
    /// parameter), because some databases require a constant. Not supported by
    /// SQL Server.
    /// </summary>
    public static AnalyticNthValueFunction NthValue(object expr, int n) =>
        new(Resolve(expr), n);

    /// <summary>
    /// The <c>NTILE(buckets)</c> analytic function: distributes the ordered rows
    /// of each window partition into <paramref name="buckets"/> ranked groups.
    /// The bucket count is emitted as an integer literal (not a bind parameter),
    /// because some databases require a constant.
    /// </summary>
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
    public static NullifFunction Nullif(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));

    /// <summary>
    /// The <c>NVL(expr1, expr2)</c> function: returns <paramref name="expr1"/> when
    /// it is not <see langword="null"/>, otherwise <paramref name="expr2"/>.
    /// </summary>
    /// <param name="expr1">The expression returned when it is not <see langword="null"/>.</param>
    /// <param name="expr2">The fallback returned when <paramref name="expr1"/> is <see langword="null"/>.</param>
    /// <returns>An <c>NVL</c> function expression.</returns>
    /// <remarks>Oracle syntax. For the standard equivalent use
    /// <see cref="Coalesce(object, object, object[])"/>.</remarks>
    public static NvlFunction Nvl(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));
}
