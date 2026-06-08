namespace SqlArtisan.Internal;

public interface IInsertBuilderValues : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends another row to the <c>VALUES</c> clause, producing a multi-row
    /// insert (<c>VALUES (...), (...)</c>). Supported by PostgreSQL, MySQL, SQLite,
    /// and SQL Server; Oracle does not support multi-row <c>VALUES</c>.
    /// </summary>
    IInsertBuilderValues Values(params object[] values);
}
