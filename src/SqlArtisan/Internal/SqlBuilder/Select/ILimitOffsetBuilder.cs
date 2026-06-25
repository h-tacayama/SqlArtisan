namespace SqlArtisan.Internal;

/// <summary>The state after <c>LIMIT n</c>: optionally add an <c>OFFSET</c> (PostgreSQL/MySQL/SQLite), or build.</summary>
public interface ILimitOffsetBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>OFFSET m</c> after <c>LIMIT n</c>. Dialect-specific
    /// (PostgreSQL / MySQL / SQLite).
    /// </summary>
    ISqlBuilder Offset(int start);
}
