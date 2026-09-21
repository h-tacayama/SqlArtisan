namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "EXCLUDED";

    // T-SQL requires MERGE to end with a semicolon; omitting it is a syntax error.
    public string MergeTerminator => ";";

    public char ParameterMarker => '@';
}
