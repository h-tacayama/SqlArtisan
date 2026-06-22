namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DbmsName => "Oracle";

    // Oracle rejects AS on a table alias (ORA-00933), so the alias follows the
    // table name separated only by a space.
    public string DmlTableAliasSeparator => " ";

    public char ParameterMarker => ':';

    // Oracle has no ON CONFLICT / ON DUPLICATE KEY UPDATE construct, so reaching
    // this is wrong-DBMS usage. Emit the canonical token faithfully (ADR 0001) and
    // let the database reject the statement rather than throwing at build time.
    public string ExcludedName => "EXCLUDED";

    public bool SupportsRollup => true;

    public bool UsesWithRollupSuffix => false;

    public bool SupportsCube => true;

    public bool SupportsGroupingSets => true;

    // Oracle's MERGE needs no terminating token.
    public string MergeTerminator => "";
}
