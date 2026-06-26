namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    // standard_conforming_strings is on by default (9.1+), so a backslash is a
    // literal backslash and is never doubled.
    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public char ParameterMarker => ':';

    public string ExcludedName => "EXCLUDED";

    // PostgreSQL's MERGE (15+) needs no terminating token.
    public string MergeTerminator => "";
}
