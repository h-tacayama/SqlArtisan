namespace SqlArtisan.Internal;

/// <summary>The state after <c>LIMIT n</c>: optionally add an <c>OFFSET</c> (PostgreSQL/MySQL/SQLite), or build.</summary>
public interface ILimitOffsetBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>OFFSET m</c> after <c>LIMIT n</c>. Dialect-specific
    /// (PostgreSQL / MySQL / SQLite).
    /// </summary>
    /// <param name="start">The number of leading rows to skip.</param>
    /// <returns>The terminal builder; call <see cref="ISqlBuilder.Build()"/>.</returns>
    ISqlBuilder Offset(int start);
}
