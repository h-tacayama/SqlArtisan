namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    // SQLite has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
