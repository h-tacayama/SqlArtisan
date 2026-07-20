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
    Unknown = 0,

    /// <summary>
    /// MySQL — backtick-quoted identifiers and <c>?</c>-prefixed parameter markers.
    /// </summary>
    MySql = 1,

    /// <summary>
    /// Oracle Database — double-quoted identifiers and <c>:</c>-prefixed parameter markers.
    /// </summary>
    Oracle = 2,

    /// <summary>
    /// PostgreSQL — double-quoted identifiers and <c>:</c>-prefixed parameter markers; the default dialect (<see cref="SqlArtisanConfig.DefaultDbms"/>).
    /// </summary>
    PostgreSql = 3,

    /// <summary>
    /// SQLite — double-quoted identifiers and <c>:</c>-prefixed parameter markers.
    /// </summary>
    Sqlite = 4,

    /// <summary>
    /// SQL Server — double-quoted identifiers and <c>@</c>-prefixed parameter markers.
    /// </summary>
    SqlServer = 5,
}
