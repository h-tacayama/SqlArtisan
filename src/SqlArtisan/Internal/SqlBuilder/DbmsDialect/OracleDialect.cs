namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    // Unused, inert sentinel — not a real Oracle limit. Oracle has no multi-row
    // VALUES, so the bulk-insert path rejects it (SupportsMultiRowValues is false)
    // before ever reading this; zero exists only to satisfy the interface.
    public int MaxParameters => 0;

    public bool SupportsMultiRowValues => false;

    // Oracle has no ON CONFLICT / ON DUPLICATE KEY UPDATE construct, so reaching
    // this is wrong-DBMS usage. Emit the canonical token faithfully (ADR 0001) and
    // let the database reject the statement rather than throwing at build time.
    public string ExcludedName => "EXCLUDED";
}
