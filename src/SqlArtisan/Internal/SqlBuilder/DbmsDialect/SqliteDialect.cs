namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public string DbmsName => "SQLite";

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    // SQLite supports no GROUP BY grouping extensions.
    public bool SupportsRollup => false;

    public bool UsesWithRollupSuffix => false;

    public bool SupportsCube => false;

    public bool SupportsGroupingSets => false;

    // SQLite has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
