namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    char ParameterMarker { get; }

    // The ceiling function's name. SQL Server only spells it CEILING; the other
    // four DBMS use CEIL (MySQL/PostgreSQL accept both).
    string CeilFunctionName { get; }
}
