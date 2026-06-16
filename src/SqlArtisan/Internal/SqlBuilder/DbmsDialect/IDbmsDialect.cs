namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    char ParameterMarker { get; }

    /// <summary>
    /// The maximum number of bind parameters a single statement may carry, used
    /// to split a collection-driven bulk <c>INSERT</c> into multiple batches
    /// (<c>BuildBatches</c>): SQL Server 2100, SQLite 999, PostgreSQL/MySQL
    /// 65535. Oracle has no multi-row <c>VALUES</c>, so its value is unused —
    /// the bulk path throws before consulting it.
    /// </summary>
    int MaxParameters { get; }

    /// <summary>
    /// Whether the DBMS supports the multi-row <c>VALUES (...),(...)</c> table
    /// value constructor — the form the collection-driven bulk <c>INSERT</c>
    /// (<c>BuildBatches</c>) emits. True for PostgreSQL, MySQL, SQLite, and SQL
    /// Server; false for Oracle, which has no multi-row <c>VALUES</c> (it can
    /// still bulk-insert via array binding / <c>INSERT ALL</c>, handled elsewhere).
    /// </summary>
    bool SupportsMultiRowValues { get; }

    /// <summary>
    /// The name that refers to the row proposed for insertion inside an
    /// UPSERT update clause: <c>EXCLUDED</c> (PostgreSQL), <c>excluded</c>
    /// (SQLite), or the row alias <c>new</c> (MySQL). Oracle and SQL Server have
    /// no such construct; they emit the canonical <c>EXCLUDED</c> token, leaving
    /// the (wrong-DBMS) statement for the database to reject.
    /// </summary>
    string ExcludedName { get; }
}
