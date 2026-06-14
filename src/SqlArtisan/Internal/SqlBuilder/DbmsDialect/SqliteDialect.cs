namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.Sqlite;

    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string OnConflictExcludedAlias => "excluded";
}
