namespace SqlArtisan.IntegrationTests.Infrastructure;

// #679: T-SQL reads a backslash before a newline in a string constant as a line
// continuation. Raw SQL and what it returns, run on both SQL Server lanes.
public static class BackslashNewlineLiterals
{
    public static TheoryData<string, string> Every => new()
    {
        { "SELECT 'a\\\r\nb'", "ab" },
        { "SELECT 'a\\\nb'", "ab" },
        { "SELECT N'a\\\nb'", "ab" },
        { "SELECT N'a\\\r\nb'", "ab" },
        { "SELECT JSON_VALUE('{\"ab\":\"other\"}', '$.\"a\\\nb\"')", "other" },
        { "SELECT JSON_QUERY('{\"ab\":{\"x\":1}}', '$.\"a\\\nb\"')", "{\"x\":1}" },
        {
            "SELECT STRING_AGG(v, 'a\\\r\nb') WITHIN GROUP (ORDER BY v) "
                + "FROM (VALUES ('x'), ('y')) t (v)",
            "xaby"
        },
    };

    // The spelling SqlArtisan writes: a spare backslash-newline ahead of the caller's,
    // which the one-pass continuation drops. A lone CR continues nothing.
    public static TheoryData<string, string> Kept => new()
    {
        { "SELECT 'a\\\\\nb'", "a\\b" },
        { "SELECT 'a\\\\\n\nb'", "a\\\nb" },
        { "SELECT 'a\\\\\r\n\r\nb'", "a\\\r\nb" },
        { "SELECT N'a\\\\\n\nb'", "a\\\nb" },
        { "SELECT 'a\\\\\\\n\nb'", "a\\\\\nb" },
        { "SELECT 'a\\\rb'", "a\\\rb" },
    };
}
