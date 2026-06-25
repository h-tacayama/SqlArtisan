namespace SqlArtisan.Internal;

/// <summary>
/// A <c>DO UPDATE SET ...</c> action that can be narrowed with a
/// <c>WHERE</c> predicate (PostgreSQL/SQLite).
/// </summary>
public interface IInsertBuilderDoUpdateSet : IReturning
{
    /// <summary>Appends <c>WHERE condition</c> to the <c>DO UPDATE SET</c>, applying the update only to conflicting rows that satisfy it.</summary>
    /// <param name="condition">The predicate gating the update; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IReturning Where(SqlCondition condition);
}
