namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED ... INSERT</c>, with a column list or
/// positional (<c>ThenInsert()</c>): supply the <c>VALUES (...)</c> list. Values are
/// any expression — typically source columns; literals are auto-parameterized.
/// </summary>
public interface IMergeBuilderThenInsert
{
    /// <summary>
    /// Appends <c>VALUES (...)</c> for the columns named by the preceding <c>INSERT</c>.
    /// </summary>
    /// <param name="values">The row values, one per inserted column; must be non-empty — any expression, typically source columns (literals are auto-parameterized).</param>
    /// <returns>The builder positioned to append Oracle's <c>WHERE</c>, chain another <c>WHEN</c> branch, or build.</returns>
    IMergeBuilderValues Values(params object[] values);
}
