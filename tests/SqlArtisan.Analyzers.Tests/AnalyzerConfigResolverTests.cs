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

    [Fact]
    public void ResolveTargetVersion_Unset_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.Null(AnalyzerConfigResolver.ResolveTargetVersion(options));
    }

    [Fact]
    public void ResolveTargetVersion_EditorConfigValue_IsParsed()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetVersionKey] = "8.0.16",
        });

        Assert.Equal("8.0.16", AnalyzerConfigResolver.ResolveTargetVersion(options)?.ToString());
    }

    [Fact]
    public void ResolveTargetVersion_EditorConfigSet_WinsOverMSBuildProperty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetVersionKey] = "23",
            [AnalyzerConfigResolver.TargetVersionMSBuildPropertyKey] = "21.3",
        });

        Assert.Equal("23", AnalyzerConfigResolver.ResolveTargetVersion(options)?.ToString());
    }

    [Fact]
    public void ResolveTargetVersion_OnlyMSBuildPropertySet_IsUsedAsFallback()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetVersionMSBuildPropertyKey] = "2022",
        });

        Assert.Equal("2022", AnalyzerConfigResolver.ResolveTargetVersion(options)?.ToString());
    }

    [Fact]
    public void ResolveTargetVersion_EditorConfigValueUnparseable_FallsThroughToMSBuildProperty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetVersionKey] = "latest",
            [AnalyzerConfigResolver.TargetVersionMSBuildPropertyKey] = "2022",
        });

        Assert.Equal("2022", AnalyzerConfigResolver.ResolveTargetVersion(options)?.ToString());
    }

    [Theory]
    [InlineData("8.0.16", true)]
    [InlineData("23ai", true)]
    [InlineData("latest", false)]
    [InlineData("", false)]
    public void IsRecognizedVersionValue_MatchesParseability(string value, bool expected)
    {
        Assert.Equal(expected, AnalyzerConfigResolver.IsRecognizedVersionValue(value));
    }

    [Fact]
    public void IsFamilyPresent_NoSyntaxKey_ReturnsFalse()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgresql",
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
    // "family present" would make the family govern in projects that named no
    // dialect at all, silently dropping a legacy-configured target.
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
    public void ResolveTargets_LegacyPairBesideBlankValuedFamilyKeys_StillDesugarsLegacy()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "mysql",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql)] = string.Empty,
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.Oracle)] = string.Empty,
        });

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).Contains(TargetDbms.MySql));
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
                (AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql), "tru"),
                (AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle), "19"),
            ],
            [.. AnalyzerConfigResolver.SetSyntaxValues(options)]);
    }

    [Fact]
    public void ResolveTargets_LegacyPairAlone_DesugarsToSingleDbms()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgresql",
            [AnalyzerConfigResolver.TargetVersionKey] = "16",
        });

        DialectTargetSet set = AnalyzerConfigResolver.ResolveTargets(options);

        Assert.True(set.Contains(TargetDbms.PostgreSql));
        Assert.Equal("16", set.VersionFor(TargetDbms.PostgreSql)?.ToString());
        Assert.False(set.Contains(TargetDbms.MySql));
    }

    [Fact]
    public void ResolveTargets_NoConfigAtAll_ReturnsEmpty()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        Assert.True(AnalyzerConfigResolver.ResolveTargets(options).IsEmpty);
    }

    [Fact]
    public void ResolveTargets_FamilyPresent_IgnoresLegacyPairEvenForAnUnnamedDbms()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgresql",
            [AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle)] = "any",
        });

        DialectTargetSet set = AnalyzerConfigResolver.ResolveTargets(options);

        Assert.True(set.Contains(TargetDbms.Oracle));
        Assert.False(set.Contains(TargetDbms.PostgreSql));
    }

    [Theory]
    [InlineData("any", true, null)]
    [InlineData("ANY", true, null)]
    [InlineData("19", true, "19")]
    [InlineData("none", false, null)]
    public void ResolveTargets_SyntaxValueForms_ResolveAsExpected(string value, bool expectedPresent, string? expectedVersion)
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
    public void ResolveTargets_FamilyVisibleOnlyViaMSBuildProperty_StillGovernsOverEditorConfigLegacyPair()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgresql",
            [AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.Sqlite)] = "any",
        });

        DialectTargetSet set = AnalyzerConfigResolver.ResolveTargets(options);

        Assert.True(set.Contains(TargetDbms.Sqlite));
        Assert.False(set.Contains(TargetDbms.PostgreSql));
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
            [AnalyzerConfigResolver.TargetDbmsKey] = "postgresql",
        });

        bool succeeded = AnalyzerConfigResolver.TryEnumerateSyntaxKeys(options, out List<string> keys);

        Assert.True(succeeded);
        Assert.Equal(2, keys.Count);
        Assert.Contains("sqlartisan_syntax_postgres", keys);
    }

    [Fact]
    public void TryEnumerateSyntaxKeys_KeysThrows_ReturnsFalse()
    {
        var options = new KeysThrowingAnalyzerConfigOptions(new Dictionary<string, string>());

        bool succeeded = AnalyzerConfigResolver.TryEnumerateSyntaxKeys(options, out List<string> keys);

        Assert.False(succeeded);
        Assert.Empty(keys);
    }
}
