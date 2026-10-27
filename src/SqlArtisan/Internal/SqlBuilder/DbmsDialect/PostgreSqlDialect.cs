namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    // A plain literal's backslash depends on standard_conforming_strings, which any
    // role or connection can turn off and so let \' close the literal; E'...'
    // reads it the same way under either setting.
    public bool BackslashLiteralsUseEscapeString => true;

    public bool BackslashNewlineContinuesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "EXCLUDED";

    public string MergeTerminator => "";

    public bool NonAsciiLiteralsUseNationalPrefix => false;

    public char ParameterMarker => ':';

    public bool ReturningIgnoresTargetAlias => false;
}
