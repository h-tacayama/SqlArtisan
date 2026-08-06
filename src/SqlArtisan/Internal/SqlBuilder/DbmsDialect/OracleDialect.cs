namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    // Oracle string literals take the backslash literally, so it is never doubled.
    public bool BackslashEscapesStringLiterals => false;

    // Oracle rejects AS on a table alias (ORA-00933), so the alias follows the
    // table name separated only by a space.
    public string DmlTableAliasSeparator => " ";

    public string ExcludedName => "EXCLUDED";

    // Oracle's MERGE needs no terminating token.
    public string MergeTerminator => "";

    public char ParameterMarker => ':';
}
