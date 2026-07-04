using System.Collections.Generic;

namespace SqlArtisan.Analyzers.Tests;

public class AnalyzerConfigResolverTests
{
    [Fact]
    public void ResolveTarget_Unset_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.Null(AnalyzerConfigResolver.ResolveTarget(options));
    }

    [Theory]
    [InlineData("mysql", "MySql")]
    [InlineData("MySQL", "MySql")]
    [InlineData("postgresql", "PostgreSql")]
    [InlineData("sqlserver", "SqlServer")]
    public void ResolveTarget_EditorConfigValue_IsCaseInsensitive(string value, string expectedName)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = value,
        });

        Assert.Equal(expectedName, AnalyzerConfigResolver.ResolveTarget(options)?.ToString());
    }

    [Fact]
    public void ResolveTarget_EditorConfigSet_WinsOverMSBuildProperty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "mysql",
            [AnalyzerConfigResolver.TargetDbmsMSBuildPropertyKey] = "oracle",
        });

        Assert.Equal(TargetDbms.MySql, AnalyzerConfigResolver.ResolveTarget(options));
    }

    [Fact]
    public void ResolveTarget_OnlyMSBuildPropertySet_IsUsedAsFallback()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsMSBuildPropertyKey] = "sqlite",
        });

        Assert.Equal(TargetDbms.Sqlite, AnalyzerConfigResolver.ResolveTarget(options));
    }

    [Fact]
    public void ResolveTarget_InvalidValue_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgres", // not "postgresql"
        });

        Assert.Null(AnalyzerConfigResolver.ResolveTarget(options));
    }

    [Theory]
    [InlineData("supported", true)]
    [InlineData("SUPPORTED", true)]
    [InlineData("unsupported", false)]
    [InlineData("nonsense", null)]
    public void ResolveOverride_Values_ParseToExpectedTriState(string value, bool? expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string> { ["key"] = value });

        Assert.Equal(expected, AnalyzerConfigResolver.ResolveOverride(options, "key"));
    }

    [Fact]
    public void ResolveOverride_KeyUnset_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.Null(AnalyzerConfigResolver.ResolveOverride(options, "key"));
    }
}
