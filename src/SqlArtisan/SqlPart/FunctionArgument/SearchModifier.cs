namespace SqlArtisan;

/// <summary>
/// The search modifier of a MySQL full-text <c>AGAINST(expr modifier)</c> clause.
/// Omit the modifier argument for MySQL's default (natural language mode).
/// </summary>
public enum SearchModifier
{
    /// <summary>
    /// Natural language search (<c>IN NATURAL LANGUAGE MODE</c>) — MySQL's default,
    /// stated explicitly.
    /// </summary>
    InNaturalLanguageMode,

    /// <summary>
    /// Boolean search (<c>IN BOOLEAN MODE</c>): the search expression uses operators
    /// such as <c>+</c>, <c>-</c>, and <c>*</c>.
    /// </summary>
    InBooleanMode,

    /// <summary>
    /// Query expansion search (<c>WITH QUERY EXPANSION</c>): a second pass reuses
    /// terms from the most relevant rows of the first.
    /// </summary>
    WithQueryExpansion,
}
