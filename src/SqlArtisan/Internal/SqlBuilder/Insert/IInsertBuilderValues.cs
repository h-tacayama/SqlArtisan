namespace SqlArtisan.Internal;

/// <summary>The state after a <c>VALUES</c> row: append more rows, add <c>RETURNING</c> or an upsert clause, or build.</summary>
public interface IInsertBuilderValues : ISqlBuilder, IReturning, IUpsert
{
    /// <summary>
    /// Appends another row to the <c>VALUES</c> clause, producing a multi-row
    /// insert (<c>VALUES (...), (...)</c>). Supported by PostgreSQL, MySQL, SQLite,
    /// and SQL Server; Oracle does not support multi-row <c>VALUES</c>.
    /// </summary>
    /// <param name="values">The row values, one per column; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c> or an upsert clause, or build.</returns>
    IInsertBuilderValues Values(params object[] values);
}
