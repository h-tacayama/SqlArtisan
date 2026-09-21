namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "excluded";

    public string MergeTerminator => "";

    public char ParameterMarker => ':';
}
