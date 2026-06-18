namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    // SQLite has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
