namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table SET</c>-style assignments: build. No upsert clause
/// and no <c>RETURNING</c>, for the reasons on <see cref="IInsertIgnoreBuilderValues"/>.
/// </summary>
public interface IInsertIgnoreBuilderSet : ISqlBuilder
{
}
