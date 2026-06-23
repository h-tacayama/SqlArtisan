namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    // SQLite renders ROLLUP in the standard function form (it has no suffix form).
    public bool UsesWithRollupSuffix => false;

    // SQLite has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
