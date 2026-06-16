namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    // PostgreSQL's wire protocol caps parameters at 65535 (16-bit count).
    public int MaxParameters => 65535;

    public bool SupportsMultiRowInsert => true;

    public string ExcludedName => "EXCLUDED";
}
