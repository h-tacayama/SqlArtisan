namespace SqlArtisan;

/// <summary>
/// A target database engine. The chosen value selects the dialect — parameter
/// markers, identifier quoting, and pagination — applied when a statement is built.
/// </summary>
public enum Dbms
{
    /// <summary>
    /// An unresolved engine; <see cref="DbmsResolver.Resolve(System.Data.IDbConnection)"/> returns this for a null or unregistered connection.
    /// </summary>
    Unknown,

    /// <summary>
    /// MySQL — backtick-quoted identifiers and <c>?</c>-prefixed parameter markers.
    /// </summary>
    MySql,

    /// <summary>
    /// Oracle Database — double-quoted identifiers and <c>:</c>-prefixed parameter markers.
    /// </summary>
    Oracle,

    /// <summary>
    /// PostgreSQL — double-quoted identifiers and <c>:</c>-prefixed parameter markers; the default dialect (<see cref="SqlArtisanConfig.DefaultDbms"/>).
    /// </summary>
    PostgreSql,

    /// <summary>
    /// SQLite — double-quoted identifiers and <c>:</c>-prefixed parameter markers.
    /// </summary>
    Sqlite,

    /// <summary>
    /// SQL Server — double-quoted identifiers and <c>@</c>-prefixed parameter markers.
    /// </summary>
    SqlServer,
}
