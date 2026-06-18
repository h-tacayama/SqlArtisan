namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    char ParameterMarker { get; }

    /// <summary>
    /// The name that refers to the row proposed for insertion inside an
    /// UPSERT update clause: <c>EXCLUDED</c> (PostgreSQL), <c>excluded</c>
    /// (SQLite), or the row alias <c>new</c> (MySQL). Oracle and SQL Server have
    /// no such construct; they emit the canonical <c>EXCLUDED</c> token, leaving
    /// the (wrong-DBMS) statement for the database to reject.
    /// </summary>
    string ExcludedName { get; }

    /// <summary>
    /// The token appended at the very end of a statement that requires explicit
    /// termination. SQL Server's <c>MERGE</c> mandates a trailing semicolon
    /// (<c>;</c>); every other dialect leaves this empty and lets the caller or
    /// driver delimit statements.
    /// </summary>
    string StatementTerminator { get; }
}
