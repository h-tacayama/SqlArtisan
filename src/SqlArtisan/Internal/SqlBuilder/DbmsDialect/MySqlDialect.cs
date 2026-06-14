namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.MySql;

    public char AliasQuote => '`';

    public char ParameterMarker => '?';

    // Unused: MySQL UPSERT is ON DUPLICATE KEY UPDATE, not ON CONFLICT.
    public string OnConflictExcludedAlias => "EXCLUDED";
}
