namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DbmsName => "PostgreSQL";

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "EXCLUDED";

    public bool SupportsRollup => true;

    public bool UsesWithRollupSuffix => false;

    public bool SupportsCube => true;

    public bool SupportsGroupingSets => true;

    // PostgreSQL's MERGE (15+) needs no terminating token.
    public string MergeTerminator => "";
}
