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
    /// <exception cref="ArgumentException">An element is a value rather than an expression or a
    /// subquery, which would bind as a constant that groups nothing.</exception>
    public static PartitionByClause PartitionBy(params object[] expressions) =>
        new(ResolveKeys(
            expressions,
            nameof(expressions),
            "PartitionByItem",
            $"{Keywords.Partition} {Keywords.By}"));

    /// <summary>
    /// The <c>PERCENTILE_CONT(fraction)</c> ordered-set aggregate (continuous
    /// percentile, interpolated). Complete it with <c>.WithinGroup(OrderBy(...))</c>.
    /// </summary>
    /// <param name="fraction">The percentile to compute, in the range 0..1.</param>
    /// <returns>A <c>PERCENTILE_CONT</c> ordered-set aggregate expression.</returns>
    /// <remarks>
    /// Dialect support is split: Oracle allows both forms, PostgreSQL only the
    /// plain <c>WithinGroup(...)</c> form, and SQL Server only the windowed
    /// <c>.Over(...)</c> form.
    /// </remarks>
    public static PercentileContFunction PercentileCont(double fraction) =>
        new(fraction);

    /// <summary>
    /// The <c>PERCENTILE_DISC(fraction)</c> ordered-set aggregate (discrete
    /// percentile, an actual value from the set). Complete it with
    /// <c>.WithinGroup(OrderBy(...))</c>.
    /// </summary>
    /// <param name="fraction">The percentile to compute, in the range 0..1.</param>
    /// <returns>A <c>PERCENTILE_DISC</c> ordered-set aggregate expression.</returns>
    /// <remarks>
    /// Dialect support is split: Oracle allows both forms, PostgreSQL only the
    /// plain <c>WithinGroup(...)</c> form, and SQL Server only the windowed
    /// <c>.Over(...)</c> form.
    /// </remarks>
    public static PercentileDiscFunction PercentileDisc(double fraction) =>
        new(fraction);

    /// <summary>
    /// The <c>PERCENT_RANK()</c> analytic function: the relative rank of the current
    /// row (0..1). Complete it with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>A <c>PERCENT_RANK</c> analytic function expression.</returns>
    public static AnalyticFunction PercentRank() => new AnalyticPercentRankFunction();

    /// <summary>
    /// The PostgreSQL <c>PLAINTO_TSQUERY(text)</c> function: parses plain,
    /// unformatted <paramref name="text"/> into a text-search query (terms ANDed).
    /// Pair with a tsvector via <see cref="TsMatch(object, object)"/>.
    /// </summary>
    /// <param name="text">The plain text to parse.</param>
    /// <returns>An expression emitting
    /// <c>PLAINTO_TSQUERY(text)</c>.</returns>
    /// <remarks>PostgreSQL syntax. For text already in tsquery syntax use
    /// <see cref="ToTsquery(object)"/>.</remarks>
    public static SqlExpression PlaintoTsquery(object text) =>
        new PlaintoTsqueryFunction(null, Resolve(text, nameof(text)));

    /// <inheritdoc cref="PlaintoTsquery(object)"/>
    /// <param name="config">The text-search configuration (e.g. <c>"english"</c>),
    /// emitted as an inline string literal.</param>
    /// <param name="text">The plain text to parse.</param>
    public static SqlExpression PlaintoTsquery(string config, object text)
    {
        StringGuard.ThrowIfNullOrEmpty(config, "PLAINTO_TSQUERY requires a configuration name.");
        return new PlaintoTsqueryFunction(config, Resolve(text, nameof(text)));
    }

    /// <summary>
    /// The <c>POSITION(<paramref name="substring"/> IN <paramref name="source"/>)</c>
    /// function: the 1-based index of the first occurrence of
    /// <paramref name="substring"/> in <paramref name="source"/>, or 0 if absent.
    /// </summary>
    /// <param name="substring">The substring to search for.</param>
    /// <param name="source">The string searched.</param>
    /// <returns>An expression emitting <c>POSITION(substring IN source)</c>.</returns>
    /// <remarks>MySQL, PostgreSQL. For PostgreSQL's function-call form with the
    /// reverse argument order, see <see cref="Strpos(object, object)"/>.</remarks>
    public static SqlExpression Position(object substring, object source) =>
        new PositionFunction(
            Resolve(substring, nameof(substring)),
            Resolve(source, nameof(source)));

    /// <summary>
    /// The <c>POWER(base, exponent)</c> function: <paramref name="base"/> raised
    /// to the power of <paramref name="exponent"/>.
    /// </summary>
    /// <param name="base">The base value.</param>
    /// <param name="exponent">The exponent to raise <paramref name="base"/> to.</param>
    /// <returns>A <c>POWER</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite (3.35+), and SQL Server syntax.</remarks>
    public static SqlExpression Power(object @base, object exponent) =>
        new PowerFunction(Resolve(@base, nameof(@base)), Resolve(exponent, nameof(exponent)));

    /// <summary>
    /// A <c>n PRECEDING</c> window-frame bound (offset rows/range before the
    /// current row).
    /// </summary>
    /// <param name="offset">The number of rows or range units before the current row; non-negative.</param>
    /// <returns>A <c>PRECEDING</c> window-frame bound.</returns>
    public static FrameBound Preceding(int offset) => FrameBound.Preceding(offset);
}
