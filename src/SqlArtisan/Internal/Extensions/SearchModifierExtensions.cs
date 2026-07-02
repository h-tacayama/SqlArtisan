namespace SqlArtisan.Internal;

internal static class SearchModifierExtensions
{
    internal static string ToKeyword(this SearchModifier modifier) => modifier switch
    {
        SearchModifier.InNaturalLanguageMode =>
            $"{Keywords.In} {Keywords.Natural} {Keywords.Language} {Keywords.Mode}",
        SearchModifier.InBooleanMode =>
            $"{Keywords.In} {Keywords.Boolean} {Keywords.Mode}",
        SearchModifier.WithQueryExpansion =>
            $"{Keywords.With} {Keywords.Query} {Keywords.Expansion}",
        _ => throw new ArgumentOutOfRangeException(nameof(modifier)),
    };
}
