namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    /// <summary>
    /// Whether a single-quoted string literal treats the backslash as an escape
    /// character, so a literal backslash must be doubled — true only under MySQL's
    /// default <c>sql_mode</c>. Governs a literal-emitting position such as
    /// <c>LIKE ... ESCAPE '\'</c>.
    /// </summary>
    bool BackslashEscapesStringLiterals { get; }

    /// <summary>
    /// The separator between a DML target table and its alias: <c> AS </c>, or a
    /// single space on Oracle, which rejects <c>AS</c> there (ORA-00933). Only DML
    /// varies — the SELECT/FROM alias stays AS-less on every dialect, since Oracle
    /// forbids <c>AS</c> there too.
    /// </summary>
    string DmlTableAliasSeparator { get; }

    /// <summary>
    /// The name that refers to the row proposed for insertion inside an UPSERT
    /// update clause. Oracle and SQL Server have no such construct, so they emit
    /// the canonical <c>EXCLUDED</c> token faithfully (ADR 0001) and leave the
    /// wrong-DBMS statement for the database to reject.
    /// </summary>
    string ExcludedName { get; }

    /// <summary>
    /// The token appended after a <c>MERGE</c> statement, required only by SQL
    /// Server. It is specific to <c>MERGE</c> — SqlArtisan does not otherwise
    /// terminate statements, deferring that to the caller or driver.
    /// </summary>
    string MergeTerminator { get; }

    char ParameterMarker { get; }
}
