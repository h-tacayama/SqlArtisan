namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table SET</c>-style assignments: add <c>RETURNING</c> or build. No upsert clause — <c>INSERT IGNORE</c> already resolves duplicate-key conflicts.
/// </summary>
public interface IInsertIgnoreBuilderSet : ISqlBuilder, IReturning
{
}
