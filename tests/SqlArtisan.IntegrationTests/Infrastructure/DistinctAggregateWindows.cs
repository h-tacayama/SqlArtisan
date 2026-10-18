namespace SqlArtisan.IntegrationTests.Infrastructure;

// #662: a window on each DISTINCT aggregate SqlArtisan offers one on, as raw SQL,
// so each lane pins whether its engine runs the pairing.
public static class DistinctAggregateWindows
{
    private static readonly string[] Aggregates = ["COUNT", "SUM", "AVG"];

    private static readonly string[] Unordered =
    [
        "OVER ()",
        "OVER (PARTITION BY department_id)",
    ];

    private static readonly string[] Ordered =
    [
        "OVER (ORDER BY id)",
        "OVER (PARTITION BY department_id ORDER BY id)",
        "OVER (ORDER BY id ROWS 1 PRECEDING)",
    ];

    public static TheoryData<string> Every => Build([.. Unordered, .. Ordered]);

    public static TheoryData<string> UnorderedOnly => Build(Unordered);

    public static TheoryData<string> OrderedOnly => Build(Ordered);

    private static TheoryData<string> Build(string[] windows)
    {
        TheoryData<string> data = new();

        foreach (string aggregate in Aggregates)
        {
            foreach (string window in windows)
            {
                data.Add($"SELECT {aggregate}(DISTINCT age) {window} FROM users");
            }
        }

        return data;
    }
}
