namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string ExcludedName => "EXCLUDED";

    // PostgreSQL's MERGE (15+) needs no terminating token.
    public string MergeTerminator => "";
}
