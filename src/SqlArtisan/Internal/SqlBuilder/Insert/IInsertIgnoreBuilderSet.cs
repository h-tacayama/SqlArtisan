namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table SET</c>-style assignments: build. No upsert clause — <c>INSERT IGNORE</c> already resolves duplicate-key conflicts — and no <c>RETURNING</c>, which MySQL lacks.
/// </summary>
public interface IInsertIgnoreBuilderSet : ISqlBuilder
{
}
