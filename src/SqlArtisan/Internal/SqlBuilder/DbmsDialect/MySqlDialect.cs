namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public char AliasQuote => '`';

    // MySQL's default sql_mode treats the backslash as a string-literal escape, so
    // a literal backslash (e.g. a LIKE ESCAPE char) must be doubled.
    public bool BackslashEscapesStringLiterals => true;

    public bool BackslashLiteralsUseEscapeString => false;

    public bool BackslashNewlineContinuesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    // The 8.0.19+ row alias RowAliasClause emits, read as `new.column`.
    public string ExcludedName => "new";

    public string MergeTerminator => "";

    public bool NonAsciiLiteralsUseNationalPrefix => false;

    public char ParameterMarker => '?';

    public bool ReturningIgnoresTargetAlias => false;
}
