namespace SqlArtisan.Internal;

/// <summary>
/// A <c>DO UPDATE SET ...</c> action that can be narrowed with a
/// <c>WHERE</c> predicate (PostgreSQL/SQLite).
/// </summary>
public interface IInsertBuilderDoUpdateSet : IInsertReturning
{
    IInsertReturning Where(SqlCondition condition);
}
