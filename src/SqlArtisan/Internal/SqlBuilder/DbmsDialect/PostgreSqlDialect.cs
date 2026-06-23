namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "EXCLUDED";

    public bool UsesWithRollupSuffix => false;

    // PostgreSQL's MERGE (15+) needs no terminating token.
    public string MergeTerminator => "";
}
