namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => '@';

    // SQL Server has no ON CONFLICT / ON DUPLICATE KEY UPDATE construct, so reaching
    // this is wrong-DBMS usage. Emit the canonical token faithfully (ADR 0001) and
    // let the database reject the statement rather than throwing at build time.
    public string ExcludedName => "EXCLUDED";

    // SQL Server requires a MERGE statement to be terminated with a semicolon;
    // omitting it raises a syntax error when the statement is executed.
    public string StatementTerminator => ";";
}
