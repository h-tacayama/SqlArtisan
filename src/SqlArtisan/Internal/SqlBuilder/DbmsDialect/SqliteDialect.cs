namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public bool BackslashLiteralsUseEscapeString => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "excluded";

    public string MergeTerminator => "";

    public bool NonAsciiLiteralsUseNationalPrefix => false;

    public char ParameterMarker => ':';

    public bool ReturningIgnoresTargetAlias => true;
}
