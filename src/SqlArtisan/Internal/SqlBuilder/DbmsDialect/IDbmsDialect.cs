namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    /// <summary>
    /// Whether a single-quoted string literal treats the backslash as an escape
    /// character (true only under MySQL's default <c>sql_mode</c>), so a literal
    /// backslash in a literal-emitting position must be doubled.
    /// </summary>
    bool BackslashEscapesStringLiterals { get; }

    /// <summary>
    /// The separator between an INSERT/UPDATE/DELETE target and its alias:
    /// <c> AS </c>, or a space on Oracle (ORA-00933). A MERGE target and every
    /// FROM alias render AS-less on all dialects.
    /// </summary>
    string DmlTableAliasSeparator { get; }

    /// <summary>
    /// The name of the row proposed for insertion inside an UPSERT update clause.
    /// Oracle and SQL Server have no such construct and emit the canonical
    /// <c>EXCLUDED</c> token faithfully (ADR 0001).
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
