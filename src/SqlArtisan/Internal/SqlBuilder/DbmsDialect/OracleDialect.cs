namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.Oracle;

    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    // Unused: Oracle UPSERT is MERGE, not ON CONFLICT.
    public string OnConflictExcludedAlias => "EXCLUDED";

    public string StatementTerminator => "";
}
