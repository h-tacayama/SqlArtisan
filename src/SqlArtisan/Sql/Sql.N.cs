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

    public static NotCondition Not(SqlCondition condition) => new(condition);

    public static NotExistsCondition NotExists(ISubquery subquery) =>
        new(subquery);

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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static NullExpression Null => new();

    public static NvlFunction Nvl(
        object expr1,
        object expr2) => new(Resolve(expr1), Resolve(expr2));
}
