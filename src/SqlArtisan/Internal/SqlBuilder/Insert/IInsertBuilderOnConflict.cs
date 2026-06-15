namespace SqlArtisan.Internal;

/// <summary>
/// The action that follows <c>ON CONFLICT</c> (PostgreSQL/SQLite): either skip
/// the row (<c>DO NOTHING</c>) or update it (<c>DO UPDATE SET ...</c>).
/// </summary>
public interface IInsertBuilderOnConflict
{
    IInsertBuilderUpsert DoNothing();

    IInsertBuilderDoUpdate DoUpdate(params EqualityBasedCondition[] assignments);
}
