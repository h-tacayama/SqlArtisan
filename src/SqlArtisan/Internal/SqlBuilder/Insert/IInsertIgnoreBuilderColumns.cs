namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table (col, ...)</c>: supply rows with <c>Values(...)</c> or feed them from a <c>SELECT</c> (or a <c>WITH</c> CTE).
/// </summary>
public interface IInsertIgnoreBuilderColumns : ISqlBuilder, ISelectBuilder, IWithBuilder
{
    /// <summary>
    /// Appends a <c>VALUES (...)</c> row matching the declared column list.
    /// </summary>
    /// <param name="values">The row values, one per listed column; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(params object[] values);
}
