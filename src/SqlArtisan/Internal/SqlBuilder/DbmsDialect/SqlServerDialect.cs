namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    public bool BackslashLiteralsUseEscapeString => false;

    public bool BackslashNewlineContinuesStringLiterals => true;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "EXCLUDED";

    // T-SQL's MERGE syntax ends in a semicolon.
    public string MergeTerminator => ";";

    public bool NonAsciiLiteralsUseNationalPrefix => true;

    public char ParameterMarker => '@';

    public bool ReturningIgnoresTargetAlias => false;
}
