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
                (AnalyzerConfigResolver.SyntaxMSBuildPropertyKey(TargetDbms.MySql), "tru"),
                (AnalyzerConfigResolver.SyntaxKey(TargetDbms.Oracle), "19"),
            ],
            [.. AnalyzerConfigResolver.SetSyntaxValues(options)]);
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

    // The replacement carries the old version over — `= any` would silently shed the
    // dialect's SQLA0101 coverage — and is spelled for the surface the key was set on.
    [Theory]
    [InlineData("sqlartisan_target_dbms", "sqlartisan_syntax_postgresql = 16")]
    [InlineData("sqlartisan_target_version", "sqlartisan_syntax_postgresql = 16")]
    [InlineData(
        "build_property.SqlArtisanTargetDbms",
        "<SqlArtisanSyntaxPostgreSql>16</SqlArtisanSyntaxPostgreSql>")]
    public void RemovedKeyReplacement_CarriesDbmsAndVersionInTheKeysOwnSurface(
        string removedKey,
        string expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = "PostgreSQL",
            [AnalyzerConfigResolver.RemovedTargetVersionMSBuildPropertyKey] = "16",
        });

        Assert.Equal(
            expected,
            AnalyzerConfigResolver.RemovedKeyReplacement(options, removedKey, out bool alreadySet));
        Assert.False(alreadySet);
    }

    [Theory]
    [InlineData("postgres", "", "sqlartisan_syntax_<dbms> = <version-or-any>")]
    [InlineData("", "16", "sqlartisan_syntax_<dbms> = 16")]
    [InlineData("mysql", "latest", "sqlartisan_syntax_mysql = any")]
    public void RemovedKeyReplacement_UnreadableHalves_FallBackToPlaceholders(
        string dbms,
        string version,
        string expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = dbms,
            [AnalyzerConfigResolver.RemovedTargetVersionKey] = version,
        });

        Assert.Equal(
            expected,
            AnalyzerConfigResolver.RemovedKeyReplacement(
                options,
                AnalyzerConfigResolver.RemovedTargetDbmsKey,
                out _));
    }

    [Fact]
    public void RemovedKeyReplacement_MSBuildSurfaceWithNoDbms_UsesThePropertyPlaceholder()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetVersionMSBuildPropertyKey] = "2019",
        });

        Assert.Equal(
            "<SqlArtisanSyntax<Dbms>>2019</SqlArtisanSyntax<Dbms>>",
            AnalyzerConfigResolver.RemovedKeyReplacement(
                options,
                AnalyzerConfigResolver.RemovedTargetVersionMSBuildPropertyKey,
                out _));
    }

    // Mid-migration the family's own line is the answer: echoing the removed pair's value
    // would override the user's newer version, or re-enable a dialect they set to `none`.
    [Theory]
    [InlineData("sqlartisan_syntax_postgresql", "14", "sqlartisan_syntax_postgresql = 14")]
    [InlineData("sqlartisan_syntax_postgresql", "none", "sqlartisan_syntax_postgresql = none")]
    [InlineData(
        "build_property.SqlArtisanSyntaxPostgreSql",
        "14",
        "<SqlArtisanSyntaxPostgreSql>14</SqlArtisanSyntaxPostgreSql>")]
    public void RemovedKeyReplacement_FamilyNamesTheSameDbms_ReturnsTheFamilyLine(
        string familyKey,
        string familyValue,
        string expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetDbmsKey] = "postgresql",
            [AnalyzerConfigResolver.RemovedTargetVersionKey] = "13",
            [familyKey] = familyValue,
        });

        Assert.Equal(
            expected,
            AnalyzerConfigResolver.RemovedKeyReplacement(
                options,
                AnalyzerConfigResolver.RemovedTargetVersionKey,
                out bool alreadySet));
        Assert.True(alreadySet);
    }

    // A lone version names no DBMS, so any replacement line would have the user pick one —
    // and override the family's own value for it. Beside a family, the advice is deletion.
    [Theory]
    [InlineData("sqlartisan_syntax_postgresql", "sqlartisan_syntax_*")]
    [InlineData("build_property.SqlArtisanSyntaxPostgreSql", "<SqlArtisanSyntax*>")]
    public void RemovedKeyReplacement_NoDbmsBesideFamily_PointsAtTheFamily(
        string familyKey,
        string expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [AnalyzerConfigResolver.RemovedTargetVersionKey] = "16",
            [familyKey] = "14",
        });

        Assert.Equal(
            expected,
            AnalyzerConfigResolver.RemovedKeyReplacement(
                options,
                AnalyzerConfigResolver.RemovedTargetVersionKey,
                out bool alreadySet));
        Assert.True(alreadySet);
    }
}
