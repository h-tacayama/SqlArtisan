namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    /// <summary>
    /// The separator between a DML target table and its alias: <c> AS </c>
    /// (PostgreSQL / SQLite / MySQL / SQL Server) or a single space
    /// (Oracle, which rejects <c>AS</c> on a table alias — ORA-00933). This is
    /// distinct from the SELECT/FROM alias, which stays AS-less for every dialect
    /// because Oracle forbids <c>AS</c> there too; only DML requires <c>AS</c>
    /// where the other engines accept it (e.g. PostgreSQL's
    /// <c>INSERT INTO t AS x</c> UPSERT form).
    /// </summary>
    string DmlTableAliasSeparator { get; }

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
    /// Whether <c>ROLLUP</c> is emitted as the MySQL suffix form
    /// (<c>GROUP BY a, b WITH ROLLUP</c>) rather than the standard function form
    /// (<c>GROUP BY ROLLUP(a, b)</c>). MySQL alone uses the suffix form.
    /// </summary>
    bool UsesWithRollupSuffix { get; }

    /// <summary>
    /// The token appended after a <c>MERGE</c> statement. SQL Server's <c>MERGE</c>
    /// syntactically requires a trailing semicolon (<c>;</c>); every other dialect
    /// needs none and leaves this empty. This is specific to <c>MERGE</c> —
    /// SqlArtisan does not otherwise terminate statements, deferring that to the
    /// caller or driver.
    /// </summary>
    string MergeTerminator { get; }
}
