namespace SqlArtisan.IntegrationTests.Infrastructure;

// docs/analyzer.md: DATETRUNC's MICROSECOND by source type, run on both SQL Server lanes.
public static class DatetruncMicrosecondScales
{
    public static TheoryData<string> Accepted => new() { "datetime2(6)", "datetime2" };

    public static TheoryData<string> Rejected => new() { "datetime2(3)", "datetime" };
}
