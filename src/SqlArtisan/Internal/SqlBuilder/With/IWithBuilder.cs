namespace SqlArtisan.Internal;

/// <summary>
/// The ability to prepend a <c>WITH</c> clause (Common Table Expressions) to the <c>SELECT</c> that feeds an <c>INSERT ... SELECT</c>.
/// </summary>
public interface IWithBuilder
{
    /// <summary>
    /// Prepends <c>WITH "cte" AS (...), ...</c> before the feeding <c>SELECT</c>.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    /// <returns>The builder positioned to write the <c>SELECT</c> that draws on the CTEs.</returns>
    ISelectBuilder With(params CommonTableExpression[] ctes);

    /// <summary>
    /// Prepends <c>WITH RECURSIVE "cte" AS (...), ...</c> before the feeding <c>SELECT</c>, allowing a CTE to reference itself.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    /// <returns>The builder positioned to write the <c>SELECT</c> that draws on the CTEs.</returns>
    ISelectBuilder WithRecursive(params CommonTableExpression[] ctes);
}
