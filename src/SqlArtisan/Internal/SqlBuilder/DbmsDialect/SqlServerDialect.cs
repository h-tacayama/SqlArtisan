namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.SqlServer;

    public char AliasQuote => '"';

    public char ParameterMarker => '@';

    // Unused: SQL Server UPSERT is MERGE, not ON CONFLICT.
    public string OnConflictExcludedAlias => "EXCLUDED";
}
