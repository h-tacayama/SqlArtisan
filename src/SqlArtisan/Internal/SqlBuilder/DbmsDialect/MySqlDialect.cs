namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public char AliasQuote => '`';

    // MySQL's default sql_mode treats the backslash as a string-literal escape, so
    // a literal backslash (e.g. a LIKE ESCAPE char) must be doubled.
    public bool BackslashEscapesStringLiterals => true;

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => '?';

    // MySQL 8.0.19+ references the proposed row through a row alias rather than
    // the deprecated VALUES() function. The builder emits `... AS new` so the
    // update clause can read it as `new.column`.
    public string ExcludedName => "new";

    // MySQL has no MERGE statement, so no terminating token applies.
    public string MergeTerminator => "";
}
