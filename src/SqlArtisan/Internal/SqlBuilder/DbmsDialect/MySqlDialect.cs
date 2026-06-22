namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public char AliasQuote => '`';

    public string DbmsName => "MySQL";

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => '?';

    // MySQL 8.0.19+ references the proposed row through a row alias rather than
    // the deprecated VALUES() function. The builder emits `... AS new` so the
    // update clause can read it as `new.column`.
    public string ExcludedName => "new";

    // MySQL supports only ROLLUP, and in the suffix form
    // `GROUP BY a, b WITH ROLLUP`; it has no CUBE or GROUPING SETS.
    public bool SupportsRollup => true;

    public bool UsesWithRollupSuffix => true;

    public bool SupportsCube => false;

    public bool SupportsGroupingSets => false;

    // MySQL has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
