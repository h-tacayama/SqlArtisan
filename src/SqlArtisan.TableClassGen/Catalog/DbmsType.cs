namespace SqlArtisan.TableClassGen;

// Values match the core Dbms enum's spelling (MySql / Sqlite / SqlServer) for
// cross-file consistency.
internal enum DbmsType
{
    None = 0,
    Oracle = 1,
    PostgreSql = 2,
    MySql = 3,
    Sqlite = 4,
    SqlServer = 5,
}
