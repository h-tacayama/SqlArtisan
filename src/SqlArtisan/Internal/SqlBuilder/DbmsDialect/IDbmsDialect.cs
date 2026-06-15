namespace SqlArtisan.Internal;

internal interface IDbmsDialect
{
    char AliasQuote { get; }

    char ParameterMarker { get; }

    /// <summary>
    /// The token used to reference the row proposed for insertion inside an
    /// UPSERT update clause: <c>EXCLUDED</c> (PostgreSQL), <c>excluded</c>
    /// (SQLite), or the row alias <c>new</c> (MySQL). DBMSes without
    /// <c>ON CONFLICT</c>/<c>ON DUPLICATE KEY UPDATE</c> support throw.
    /// </summary>
    string ExcludedReference { get; }
}
