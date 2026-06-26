namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED ... INSERT (columns)</c>: supply the
/// <c>VALUES (...)</c> list. Values may be source columns or literals
/// (auto-parameterized).
/// </summary>
public interface IMergeBuilderThenInsert
{
    /// <summary>
    /// Appends <c>VALUES (...)</c> for the columns named by the preceding <c>INSERT</c>.
    /// </summary>
    /// <param name="values">The row values, one per inserted column — source columns or literals (literals are auto-parameterized).</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    IMergeBuilderOn Values(params object[] values);
}
