using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
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
