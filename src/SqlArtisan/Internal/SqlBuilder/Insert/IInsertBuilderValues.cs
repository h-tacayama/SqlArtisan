namespace SqlArtisan.Internal;

public interface IInsertBuilderValues : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends another row to the <c>VALUES</c> clause, producing a multi-row
    /// insert (<c>VALUES (...), (...)</c>). Supported by PostgreSQL, MySQL, SQLite,
    /// and SQL Server; Oracle does not support multi-row <c>VALUES</c>.
    /// </summary>
    IInsertBuilderValues Values(params object[] values);

    /// <summary>
    /// PostgreSQL / SQLite UPSERT. Follow with <c>DoNothing()</c> or
    /// <c>DoUpdateSet(...)</c>. (MySQL uses <see cref="OnDuplicateKeyUpdate"/>;
    /// Oracle / SQL Server use MERGE.)
    /// </summary>
    IOnConflictBuilder OnConflict(params DbColumn[] conflictTarget);

    /// <summary>
    /// MySQL UPSERT: <c>... AS new ON DUPLICATE KEY UPDATE col = new.col</c>.
    /// </summary>
    ISqlBuilder OnDuplicateKeyUpdate(params DbColumn[] updateColumns);
}
