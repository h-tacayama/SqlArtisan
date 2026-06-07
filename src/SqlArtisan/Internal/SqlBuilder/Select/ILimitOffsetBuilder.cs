namespace SqlArtisan.Internal;

public interface ILimitOffsetBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>OFFSET m</c> after <c>LIMIT n</c>. Dialect-specific
    /// (PostgreSQL / MySQL / SQLite).
    /// </summary>
    ISqlBuilder Offset(int start);
}
