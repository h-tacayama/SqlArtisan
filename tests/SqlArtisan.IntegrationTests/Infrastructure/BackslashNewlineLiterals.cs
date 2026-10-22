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
        { "SELECT JSON_VALUE('{\"ab\":\"other\"}', '$.\"a\\\nb\"')", "other" },
        {
            "SELECT STRING_AGG(v, 'a\\\r\nb') WITHIN GROUP (ORDER BY v) "
                + "FROM (VALUES ('x'), ('y')) t (v)",
            "xaby"
        },
    };
}
