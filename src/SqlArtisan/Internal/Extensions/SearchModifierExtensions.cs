namespace SqlArtisan.Internal;

internal static class SearchModifierExtensions
{
    internal static string ToKeyword(this SearchModifier modifier) => modifier switch
    {
        SearchModifier.InNaturalLanguageMode => Keywords.InNaturalLanguageMode,
        SearchModifier.InBooleanMode => Keywords.InBooleanMode,
        SearchModifier.WithQueryExpansion => Keywords.WithQueryExpansion,
        _ => throw new ArgumentOutOfRangeException(nameof(modifier)),
    };
}
