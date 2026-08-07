namespace SqlArtisan.Analyzers;

/// <summary>
/// Mirrors the SqlArtisan core's <c>Dbms</c> enum (minus <c>Unknown</c>, which has
/// no meaning as an analyzer target). Kept as a local copy rather than a reference
/// to the core assembly: a netstandard2.0 analyzer project cannot reference a
/// net8.0 library, so this analyzer matches SqlArtisan symbols by name instead.
/// </summary>
internal enum TargetDbms
{
    MySql,
    Oracle,
    PostgreSql,
    Sqlite,
    SqlServer,
}

/// <summary>The display spelling diagnostic messages use, shared by every rule that names a <see cref="TargetDbms"/>.</summary>
internal static class TargetDbmsNames
{
    public static string Display(TargetDbms dbms) => dbms switch
    {
        TargetDbms.MySql => "MySQL",
        TargetDbms.Oracle => "Oracle",
        TargetDbms.PostgreSql => "PostgreSQL",
        TargetDbms.Sqlite => "SQLite",
        TargetDbms.SqlServer => "SQL Server",
        _ => dbms.ToString(),
    };
}
