namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>LIMIT n</c>: optionally add an <c>OFFSET</c> (MySQL/PostgreSQL/SQLite), build, or embed as a subquery.
/// </summary>
public interface ILimitOffsetBuilder : ISqlBuilder, ISubquery
{
    /// <summary>
    /// Appends <c>OFFSET m</c> after <c>LIMIT n</c>. Dialect-specific
    /// (MySQL / PostgreSQL / SQLite).
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The terminal builder; build, or embed as a subquery.</returns>
    ISelectBuilderPaginated Offset(int start);
}
