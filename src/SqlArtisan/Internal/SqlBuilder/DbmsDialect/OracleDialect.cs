namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public bool BackslashEscapesStringLiterals => false;

    // Oracle rejects AS on a table alias (ORA-00933), so the alias follows the
    // table name separated only by a space.
    public string DmlTableAliasSeparator => " ";

    public string ExcludedName => "EXCLUDED";

    public string MergeTerminator => "";

    public char ParameterMarker => ':';
}
