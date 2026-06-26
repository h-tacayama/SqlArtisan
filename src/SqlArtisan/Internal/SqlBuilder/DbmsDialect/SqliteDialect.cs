namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    // SQLite string literals take the backslash literally, so it is never doubled.
    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    // SQLite has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
