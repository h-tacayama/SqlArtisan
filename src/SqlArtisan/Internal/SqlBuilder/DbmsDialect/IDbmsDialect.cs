namespace SqlArtisan;

internal interface IDbmsDialect
{
    string AliasQuote { get; }

    string ParameterMarker { get; }
}
