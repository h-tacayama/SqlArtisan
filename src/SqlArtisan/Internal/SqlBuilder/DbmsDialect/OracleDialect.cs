namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " ";

    public string ExcludedName => "EXCLUDED";

    public string MergeTerminator => "";

    public char ParameterMarker => ':';
}
