using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

/// <summary>
/// The MySQL full-text <c>MATCH (columns)</c> construct, pending its mandatory
/// <c>AGAINST</c> clause. Complete it with <see cref="Against(object)"/> (a
/// <c>WHERE</c> predicate) or <see cref="AgainstScore(object)"/> (the numeric
/// relevance score) before it can be used.
/// </summary>
public sealed class MatchFunction : IIncompleteExpression
{
    private readonly SqlExpression[] _columns;

    internal MatchFunction(SqlExpression[] columns)
    {
        _columns = columns;
    }

    string IIncompleteExpression.IncompleteMessage =>
        "MATCH requires an AGAINST clause; complete it with .Against(...) or .AgainstScore(...).";

    /// <summary>
    /// Supplies the mandatory <c>AGAINST</c> clause as a predicate:
    /// <c>MATCH (columns) AGAINST (searchExpr)</c>.
    /// </summary>
    /// <param name="searchExpr">The text to search for.</param>
    /// <returns>A <see cref="MatchAgainstCondition"/> for a <c>WHERE</c> clause.</returns>
    /// <remarks>MySQL syntax.</remarks>
    public MatchAgainstCondition Against(object searchExpr) =>
        new(_columns, ResolveSearchText(searchExpr), null);

    /// <inheritdoc cref="Against(object)"/>
    /// <param name="searchExpr">The text to search for.</param>
    /// <param name="modifier">The search modifier (e.g.
    /// <see cref="SearchModifier.InBooleanMode"/>), emitted after the text.</param>
    /// <remarks>MySQL syntax.</remarks>
    public MatchAgainstCondition Against(object searchExpr, SearchModifier modifier) =>
        new(_columns, ResolveSearchText(searchExpr), modifier);

    /// <summary>
    /// Supplies the mandatory <c>AGAINST</c> clause as a value — the relevance
    /// score: <c>MATCH (columns) AGAINST (searchExpr)</c>, usable in a select list
    /// or <c>ORDER BY</c>.
    /// </summary>
    /// <param name="searchExpr">The text to search for.</param>
    /// <returns>A <see cref="MatchAgainstExpression"/> emitting the relevance score.</returns>
    /// <remarks>MySQL syntax.</remarks>
    public MatchAgainstExpression AgainstScore(object searchExpr) =>
        new(_columns, Resolve(searchExpr), null);

    /// <inheritdoc cref="AgainstScore(object)"/>
    /// <param name="searchExpr">The text to search for.</param>
    /// <param name="modifier">The search modifier (e.g.
    /// <see cref="SearchModifier.InBooleanMode"/>), emitted after the text.</param>
    /// <remarks>MySQL syntax.</remarks>
    public MatchAgainstExpression AgainstScore(object searchExpr, SearchModifier modifier) =>
        new(_columns, Resolve(searchExpr), modifier);

    // MySQL 8.0 runs AGAINST (NULL) and matches no row, so neither Sql.Null nor a NULL
    // test is a remedy for the predicate (#614).
    private static SqlExpression ResolveSearchText(object searchExpr) =>
        searchExpr is null
            ? throw new ArgumentNullException(
                nameof(searchExpr),
                "AGAINST cannot take a C# null, and a NULL search text matches no row; "
                    + "pass the text, or leave the predicate out.")
            : Resolve(searchExpr);
}
