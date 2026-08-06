namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    // SQL Server string literals take the backslash literally, so it is never doubled.
    public bool BackslashEscapesStringLiterals => false;

    public string DmlTableAliasSeparator => " AS ";

    public string ExcludedName => "EXCLUDED";

    // SQL Server requires a MERGE statement to be terminated with a semicolon;
    // omitting it raises a syntax error when the statement is executed.
    public string MergeTerminator => ";";

    public char ParameterMarker => '@';
}
