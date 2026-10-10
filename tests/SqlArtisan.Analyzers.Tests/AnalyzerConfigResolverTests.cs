using System.Collections.Generic;

namespace SqlArtisan.Analyzers.Tests;

public class AnalyzerConfigResolverTests
{
    [Theory]
    [InlineData("supported", true)]
    [InlineData("SUPPORTED", true)]
    [InlineData("unsupported", false)]
    [InlineData("nonsense", null)]
    public void ResolveOverride_Values_ParseToExpectedTriState(string value, bool? expected)
    {
        var options = new TestAnalyzerConfigOptions(
            new Dictionary<string,
            string>
            { ["key"] = value });

        Assert.Equal(expected, AnalyzerConfigResolver.ResolveOverride(options, "key"));
    }

    [Fact]
    public void ResolveOverride_KeyUnset_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.Null(AnalyzerConfigResolver.ResolveOverride(options, "key"));
    }

    [Fact]
    public void IsFamilyPresent_NoSyntaxKey_ReturnsFalse()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = "postgresql",
        });

        Assert.False(AnalyzerConfigResolver.IsFamilyPresent(options));
    }

    [Fact]
    public void IsFamilyPresent_OneSyntaxKeyPresent_ReturnsTrueEvenIfInvalid()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "nonsense",
        });

        Assert.True(AnalyzerConfigResolver.IsFamilyPresent(options));
    }

    // The SDK emits a key for every declared CompilerVisibleProperty, with an
    // empty value when the consumer never set one — so the five properties the
    // shipped props declares reach every package consumer. Reading those as
    // "family present" would configure the analyzer in projects that named no
    // dialect at all.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsFamilyPresent_BlankValuedKeys_ReadAsUnset(string blank)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.Oracle)] = blank,
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.MySql)] = blank,
        });

        Assert.False(AnalyzerConfigResolver.IsFamilyPresent(options));
    }

    [Fact]
    public void SetSyntaxValues_SkipsBlanksAndReadsBothSurfaces()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "19",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql)] = "tru",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.Sqlite)] = string.Empty,
        });

        Assert.Equal(
            [
                (
                    TargetDbms.MySql,
                    AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql),
                    "tru"),
                (TargetDbms.Oracle, AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle), "19"),
            ],
            [.. AnalyzerConfigResolver.SetSyntaxValues(options)]);
    }

    // The SDK emits every declared property blank, so only a blank .editorconfig key was
    // written by someone.
    [Fact]
    public void BlankSyntaxKeys_ReadsTheEditorConfigSurfaceOnly()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Sqlite)] = string.Empty,
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "19",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql)] = string.Empty,
        });

        Assert.Equal([TargetDbms.Sqlite], [.. AnalyzerConfigResolver.BlankSyntaxKeys(options)]);
    }

    [Theory]
    [InlineData("SqlServer", "2022", true)]
    [InlineData("SqlServer", "2000", true)]
    [InlineData("SqlServer", "16", false)]
    [InlineData("SqlServer", "1999", false)]
    [InlineData("PostgreSql", "16", true)]
    [InlineData("PostgreSql", "2022", false)]
    [InlineData("MySql", "8.0.16", true)]
    [InlineData("Oracle", "23ai", true)]
    [InlineData("Sqlite", "999", true)]
    [InlineData("Sqlite", "1000", false)]
    [InlineData("SqlServer", "ANY", true)]
    public void IsRecognizedSyntaxValue_VersionSpelling_ReadsYearsOnSqlServerOnly(
        string dbms,
        string value,
        bool expected)
    {
        Assert.Equal(
            expected,
            AnalyzerConfigResolver.IsRecognizedSyntaxValue(Enum.Parse<TargetDbms>(dbms), value));
    }

    [Fact]
    public void ResolveTargets_RemovedPairAlone_ReturnsEmpty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = "postgresql",
            [AnalyzerConfigResolver.RemovedTargetVersionKey] = "16",
            [AnalyzerConfigResolver.RemovedTargetDbmsMSBuildPropertyKey] = "oracle",
        });

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).IsEmpty);
    }

    [Fact]
    public void ResolveTargets_NoConfigAtAll_ReturnsEmpty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).IsEmpty);
    }

    [Theory]
    [InlineData("any", true, null)]
    [InlineData("ANY", true, null)]
    [InlineData("19", true, "19")]
    [InlineData("none", false, null)]
    public void ResolveTargets_SyntaxValueForms_ResolveAsExpected(
        string value,
        bool expectedPresent,
        string? expectedVersion)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = value,
        });

        DialectTargetSet set = AnalyzerConfigResolver.ResolveTargets(options);

        Assert.Equal(expectedPresent, set.Contains(TargetDbms.Oracle));
        Assert.Equal(expectedVersion, set.VersionFor(TargetDbms.Oracle)?.ToString());
    }

    [Fact]
    public void ResolveTargets_SyntaxKeyUnrecognizedValue_TreatsDbmsAsUnset()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "tru",
        });

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).IsEmpty);
    }

    [Fact]
    public void ResolveTargets_EditorConfigSyntaxKey_WinsOverMSBuildPropertyPerDbms()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.MySql)] = "any",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql)] = "none",
        });

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).Contains(TargetDbms.MySql));
    }

    [Fact]
    public void ResolveTargets_NoneInEditorConfig_OverridesMSBuildPropertyDeclaredDbms()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.MySql)] = "none",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql)] = "8.0",
        });

        Assert.False(AnalyzerConfigResolver.ResolveTargets(options).Contains(TargetDbms.MySql));
    }

    [Fact]
    public void ResolveTargets_InvalidEditorConfigValue_FallsThroughToMSBuildProperty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "tru",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.Oracle)] = "19",
        });

        DialectTargetSet set = AnalyzerConfigResolver.ResolveTargets(options);

        Assert.True(set.Contains(TargetDbms.Oracle));
        Assert.Equal("19", set.VersionFor(TargetDbms.Oracle)?.ToString());
    }

    [Fact]
    public void TryEnumerateSyntaxKeys_ReturnsOnlyThePrefixedKeys()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "any",
            ["sqlartisan_syntax_postgres"] = "16", // typo'd DBMS name
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = "postgresql",
        });

        bool succeeded = AnalyzerConfigResolver.TryEnumerateSyntaxKeys(
            options,
            out List<string> keys);

        Assert.True(succeeded);
        Assert.Equal(2, keys.Count);
        Assert.Contains("sqlartisan_syntax_postgres", keys);
    }

    [Fact]
    public void TryEnumerateSyntaxKeys_KeysThrows_ReturnsFalse()
    {
        var options = new KeysThrowingAnalyzerConfigOptions(new Dictionary<string, string>());

        bool succeeded = AnalyzerConfigResolver.TryEnumerateSyntaxKeys(
            options,
            out List<string> keys);

        Assert.False(succeeded);
        Assert.Empty(keys);
    }

    // Fixed per surface, never derived from the file's config: a per-file line contradicted
    // itself across .editorconfig scopes, since a location-less report cannot say which file.
    [Theory]
    [InlineData("sqlartisan_target_dbms", "sqlartisan_syntax_<dbms>")]
    [InlineData("sqlartisan_target_version", "sqlartisan_syntax_<dbms>")]
    [InlineData("build_property.SqlArtisanTargetDbms", "<SqlArtisanSyntax<Dbms>>")]
    [InlineData("build_property.SqlArtisanTargetVersion", "<SqlArtisanSyntax<Dbms>>")]
    public void RemovedKeyReplacement_IsSpelledForTheKeysSurface(string removedKey, string expected)
    {
        Assert.Equal(expected, AnalyzerConfigResolver.RemovedKeyReplacement(removedKey));
    }
}
