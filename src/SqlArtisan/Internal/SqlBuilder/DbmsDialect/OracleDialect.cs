namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public bool BackslashLiteralsUseEscapeString => false;

    public bool BackslashNewlineContinuesStringLiterals => false;

    public string DmlTableAliasSeparator => " ";

    public string ExcludedName => "EXCLUDED";

    public string MergeTerminator => "";

    public bool NonAsciiLiteralsUseNationalPrefix => false;

    public char ParameterMarker => ':';

    public bool QualifiedAsteriskDropsSchema => false;

    public bool ReturningIgnoresTargetAlias => false;
}
