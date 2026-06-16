namespace SqlArtisan.Internal;

/// <summary>
/// The collection-driven bulk-insert terminal: builds a multi-row
/// <c>INSERT ... VALUES (...),(...),...</c> and automatically splits it into
/// several statements so no single one exceeds the target dialect's bind-parameter
/// cap (SQL Server 2100, SQLite 999, PostgreSQL/MySQL 65535).
/// </summary>
/// <remarks>
/// Unlike <see cref="ISqlBuilder.Build()"/>, this returns <em>multiple</em>
/// statements; execute every element. Splitting is by the rows' parameters; a
/// fixed-parameter tail such as <c>ON CONFLICT ... DO UPDATE SET col = :p</c> is
/// not counted, so keep a small margin below the cap if you add one. Oracle has
/// no multi-row <c>VALUES</c> and is rejected with a
/// <see cref="System.NotSupportedException"/>.
/// </remarks>
public interface IBulkInsert
{
    /// <summary>
    /// Builds the batched statements for the default DBMS
    /// (<see cref="SqlArtisanConfig.DefaultDbms"/>).
    /// </summary>
    IReadOnlyList<SqlStatement> BuildBatches();

    /// <summary>
    /// Builds the batched statements for the given <paramref name="dbms"/>.
    /// </summary>
    IReadOnlyList<SqlStatement> BuildBatches(Dbms dbms);
}
