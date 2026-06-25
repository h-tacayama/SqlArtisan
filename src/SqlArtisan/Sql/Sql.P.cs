using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// A <c>PARTITION BY</c> list for a window/analytic function's <c>OVER(...)</c>
    /// clause, emitted as <c>PARTITION BY a, b</c>.
    /// </summary>
    /// <param name="expressions">The columns or expressions to partition by.</param>
    /// <returns>A <c>PARTITION BY</c> clause.</returns>
    public static PartitionByClause PartitionBy(
        params object[] expressions) => new(Resolve(expressions));

    /// <summary>
    /// The <c>PERCENTILE_CONT(fraction)</c> ordered-set aggregate (continuous
    /// percentile, interpolated). Complete it with <c>.WithinGroup(OrderBy(...))</c>.
    /// The fraction (0..1) is emitted as a literal, because some databases
    /// require a constant. Dialect support is split: Oracle allows both forms,
    /// PostgreSQL only the plain <c>WithinGroup(...)</c> form, and SQL Server
    /// only the windowed <c>.Over(...)</c> form.
    /// </summary>
    public static PercentileContFunction PercentileCont(double fraction) =>
        new(fraction);

    /// <summary>
    /// The <c>PERCENTILE_DISC(fraction)</c> ordered-set aggregate (discrete
    /// percentile, an actual value from the set). Complete it with
    /// <c>.WithinGroup(OrderBy(...))</c>. The fraction (0..1) is emitted as a
    /// literal, because some databases require a constant. Dialect support is
    /// split: Oracle allows both forms, PostgreSQL only the plain
    /// <c>WithinGroup(...)</c> form, and SQL Server only the windowed
    /// <c>.Over(...)</c> form.
    /// </summary>
    public static PercentileDiscFunction PercentileDisc(double fraction) =>
        new(fraction);

    /// <summary>
    /// The <c>PERCENT_RANK()</c> analytic function: the relative rank of the current
    /// row (0..1). Complete it with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>A <c>PERCENT_RANK</c> analytic function expression.</returns>
    public static AnalyticPercentRankFunction PercentRank() => new();

    /// <summary>
    /// The <c>POWER(base, exponent)</c> function: <paramref name="base"/> raised
    /// to the power of <paramref name="exponent"/>.
    /// </summary>
    public static PowerFunction Power(
        object @base,
        object exponent) => new(
            Resolve(@base),
            Resolve(exponent));

    /// <summary>
    /// A <c>n PRECEDING</c> window-frame bound (offset rows/range before the
    /// current row).
    /// </summary>
    public static FrameBound Preceding(int offset) => FrameBound.Preceding(offset);
}
