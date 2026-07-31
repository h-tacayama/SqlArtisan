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
    /// emitted exactly as written, so spell it in the target dialect's syntax
    /// (e.g. Oracle <c>/*+ ... */</c>, SQL Server <c>OPTION (...)</c> or table
    /// hints).
    /// </summary>
    /// <param name="hints">The hint text, emitted verbatim into the statement.</param>
    /// <returns>A hint clause for <c>Sql.Select(hints, ...)</c>.</returns>
    public static SqlHints Hints(string hints) => new(hints);
}
