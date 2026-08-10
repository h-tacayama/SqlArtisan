using System.Collections.Generic;
using System.Linq;

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

    // Shared by every rule that joins more than one failing dialect into a single
    // message (SQLA0100, SQLA0104): "MySQL", "MySQL and Oracle", "MySQL, Oracle and PostgreSQL".
    public static string JoinDisplayNames(IReadOnlyList<string> names) => names.Count switch
    {
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1],
    };
}
