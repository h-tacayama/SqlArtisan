using System.Data;
using SqlArtisan.Analyzers;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// Builds names at and one character past SQLA0103's limit for a dialect, so each
/// per-engine twin checks the limit the analyzer reads rather than a copy of it.
/// </summary>
internal static class IdentifierLimits
{
    // One single-byte and one three-byte character, so a byte limit and a
    // character limit come apart.
    public static TheoryData<char> Characters => new() { 'a', 'あ' };

    public static string AtTheLimit(TargetDbms dbms, char character)
    {
        DialectIdentifierLimit limit = IdentifierLengthLimits.For(dbms)!.Value;
        string name = string.Empty;
        while (IdentifierLengthLimits.Measure(name + character, limit.Unit) <= limit.Limit)
        {
            name += character;
        }

        return name;
    }

    // The column name the engine hands back for an alias the library emitted.
    public static string ReturnedAlias(IDbConnection connection, string alias)
    {
        UsersTable u = new();
        IDictionary<string, object> row = (IDictionary<string, object>)connection
            .Query(Select(u.Id.As(alias)).From(u))
            .First();

        return row.Keys.Single();
    }
}
