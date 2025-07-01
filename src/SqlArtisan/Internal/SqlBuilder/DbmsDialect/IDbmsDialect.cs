namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    string AliasQuote { get; }

    string ParameterMarker { get; }
}
