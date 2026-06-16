namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    // SQLITE_MAX_VARIABLE_NUMBER defaults to 999 before SQLite 3.32.0 (and on
    // many distro builds since); the conservative value avoids a hard error.
    public int MaxParameters => 999;

    public bool SupportsMultiRowInsert => true;

    public string ExcludedName => "excluded";
}
