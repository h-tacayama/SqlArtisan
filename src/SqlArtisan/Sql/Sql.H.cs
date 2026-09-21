using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>(leftBits &lt;~&gt; rightBits)</c> Hamming distance operator between
    /// two bit vectors (PostgreSQL). Requires the pgvector extension (0.7.0+).
    /// </summary>
    /// <param name="leftBits">The first bit vector.</param>
    /// <param name="rightBits">The second bit vector.</param>
    /// <returns>A <c>&lt;~&gt;</c> operator expression.</returns>
    public static HammingDistanceOperator HammingDistance(object leftBits, object rightBits) =>
        new(Resolve(leftBits), Resolve(rightBits));

    /// <summary>
    /// An optimizer-hint clause carrying <paramref name="hints"/> verbatim, to
    /// pass as the first argument of <c>Sql.Select(hints, ...)</c>. The string is
    /// emitted exactly as written, immediately after <c>SELECT</c> — the slot
    /// Oracle's and MySQL's <c>/*+ ... */</c> hints occupy. A hint whose grammar
    /// sits elsewhere (SQL Server's trailing <c>OPTION (...)</c>, its table
    /// hints) does not belong here.
    /// </summary>
    /// <param name="hints">The hint text, emitted verbatim into the statement.</param>
    /// <returns>A hint clause for <c>Sql.Select(hints, ...)</c>.</returns>
    public static SqlHints Hints(string hints) => new(hints);

    /// <summary>
    /// The <c>HOUR</c> interval field, for the sole-field overload of
    /// <see cref="IntervalLiteral(string, IntervalField)"/> or as the leading
    /// field of <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (e.g. <c>HOUR TO SECOND</c>).
    /// </summary>
    /// <param name="precision">The leading field's digit count (0-9); omit for
    /// Oracle's own default of 2.</param>
    /// <returns>An <see cref="IntervalField"/> emitting <c>HOUR</c> or <c>HOUR(precision)</c>.</returns>
    public static IntervalField Hour(int? precision = null) => new(DateTimePart.Hour, precision);
}
