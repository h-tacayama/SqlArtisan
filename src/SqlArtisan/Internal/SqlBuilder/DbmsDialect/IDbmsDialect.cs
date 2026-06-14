namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    Dbms Dbms { get; }

    char AliasQuote { get; }

    char ParameterMarker { get; }
}
