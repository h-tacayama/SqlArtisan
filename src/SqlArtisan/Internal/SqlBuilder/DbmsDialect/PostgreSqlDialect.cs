namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    // standard_conforming_strings is on by default (9.1+), so a backslash is a
    // literal backslash and is never doubled.
    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "EXCLUDED";

    public string MergeTerminator => "";

    public char ParameterMarker => ':';
}
