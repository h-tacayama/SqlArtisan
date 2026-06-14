namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    char ParameterMarker { get; }
}
