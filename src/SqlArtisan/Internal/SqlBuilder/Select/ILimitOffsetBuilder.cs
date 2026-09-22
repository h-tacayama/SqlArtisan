namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>LIMIT n</c>: optionally add an <c>OFFSET</c> (MySQL/PostgreSQL/SQLite),
/// lock the selected rows with <c>FOR UPDATE</c>, build, or embed as a subquery.
/// </summary>
public interface ILimitOffsetBuilder : ISqlBuilder, IForUpdate, ISubquery
{
    /// <summary>
    /// Appends <c>OFFSET m</c> after <c>LIMIT n</c>. Dialect-specific
    /// (MySQL / PostgreSQL / SQLite).
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The builder positioned to optionally lock the rows with <c>FOR UPDATE</c>, build, or embed as a subquery.</returns>
    ISelectBuilderPaginated Offset(int start);
}
