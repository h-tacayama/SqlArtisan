using System.Collections.Generic;

namespace SqlArtisan.Analyzers.Tests;

public class DialectSupportResolverTests
{
    [Fact]
    public void Resolve_NoOverrideRealMatrixEntry_UsesMatrix()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(options, "Rollup", arity: null, TargetDbms.MySql);

        Assert.NotNull(result);
        Assert.False(result!.Value.IsSupported);
        Assert.Equal("sqlartisan_construct_rollup", result.Value.OverrideKeyHint);
    }

    [Fact]
    public void Resolve_MemberOverrideSupported_WinsOverMatrix()
    {
        // Rollup is MySql:false in the shipped matrix; the override forces it supported
        // (e.g. the caller has verified their own MySQL variant/version handles it).
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_rollup"] = "supported",
        });

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(options, "Rollup", arity: null, TargetDbms.MySql);

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
        Assert.False(result.Value.IsArityLevel);
    }

    [Fact]
    public void Resolve_ArityOverride_WinsOverMemberOverride()
    {
        // Synthetic member name (this test exercises the resolver's own precedence
        // logic, not a real SqlArtisan dialect fact — neither key needs a matrix entry
        // since an override short-circuits before the matrix is even consulted).
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_synthetic"] = "unsupported",
            ["sqlartisan_construct_synthetic_arity2"] = "supported",
        });

        DialectSupportResolver.Result? oneArg = DialectSupportResolver.Resolve(options, "Synthetic", arity: 1, TargetDbms.MySql);
        DialectSupportResolver.Result? twoArg = DialectSupportResolver.Resolve(options, "Synthetic", arity: 2, TargetDbms.MySql);

        Assert.False(oneArg!.Value.IsSupported);
        Assert.False(oneArg.Value.IsArityLevel);

        Assert.True(twoArg!.Value.IsSupported);
        Assert.True(twoArg.Value.IsArityLevel);
    }

    [Fact]
    public void Resolve_PropertyReferenceHasNoArity_MemberOverrideStillApplies()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_dual"] = "supported",
        });

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(options, "Dual", arity: null, TargetDbms.MySql);

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
    }

    [Fact]
    public void Resolve_UnknownMemberNoOverride_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(options, "NotInMatrix", arity: null, TargetDbms.MySql);

        Assert.Null(result);
    }

    // Rollup has no version bound recorded (DialectMatrix.AllBounds) — declaring
    // a target version must not change its verdict from the plain matrix bool.
    [Fact]
    public void Resolve_DeclaredVersionEntryHasNoBound_FallsBackToMatrixBool()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "Rollup", arity: null, TargetDbms.MySql, EngineVersion.Parse("8.0.16"));

        Assert.NotNull(result);
        Assert.False(result!.Value.IsSupported);
        Assert.False(result.Value.IsVersionBound);
        Assert.Null(result.Value.RequiredVersion);
    }

    [Fact]
    public void Resolve_NoDeclaredVersion_BehavesExactlyAsBeforeVersionBounds()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(options, "Rollup", arity: null, TargetDbms.MySql);

        Assert.NotNull(result);
        Assert.False(result!.Value.IsVersionBound);
    }

    [Fact]
    public void Resolve_MemberOverride_WinsEvenWithADeclaredVersion()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_rollup"] = "supported",
        });

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "Rollup", arity: null, TargetDbms.MySql, EngineVersion.Parse("5.7"));

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
        Assert.False(result.Value.IsVersionBound);
    }

    // No seeded bound sits on a false cell today (WithRecursive's Oracle-23
    // candidate was disproven live, #263), so a false cell with a declared
    // version keeps the plain-bool verdict — the flip direction stays covered
    // by the ADR 0015 live-proof discipline, not by a data row.
    [Fact]
    public void Resolve_FalseCellWithDeclaredVersionAndNoBound_StaysUnsupported()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "WithRecursive", arity: null, TargetDbms.Oracle, EngineVersion.Parse("23"));

        Assert.NotNull(result);
        Assert.False(result!.Value.IsSupported);
        Assert.False(result.Value.IsVersionBound);
    }

    // WithRecursive is mysql:true in the plain matrix but bound to 8.0 (no CTE
    // support before it) — a declared version below the bound must report the
    // shortfall, not fall back to the (also-true) plain bool.
    [Fact]
    public void Resolve_TrueCellWithDeclaredVersionBelowBound_ReportsVersionBoundForMySql()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "WithRecursive", arity: null, TargetDbms.MySql, EngineVersion.Parse("5.7"));

        Assert.NotNull(result);
        Assert.False(result!.Value.IsSupported);
        Assert.True(result.Value.IsVersionBound);
        Assert.Equal("8.0", result.Value.RequiredVersion);
    }

    // Datetrunc is sqlServer:true in the plain matrix but bound to 2022 — a
    // declared version below the bound must report SQLA0006, not silence.
    [Fact]
    public void Resolve_TrueCellWithDeclaredVersionBelowBound_ReportsVersionBound()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "Datetrunc", arity: null, TargetDbms.SqlServer, EngineVersion.Parse("2019"));

        Assert.NotNull(result);
        Assert.False(result!.Value.IsSupported);
        Assert.True(result.Value.IsVersionBound);
        Assert.Equal("2022", result.Value.RequiredVersion);
    }

    // Trim has both a member-level bound (2017, the 1-arg form) and a narrower
    // arity-2 bound (2022, the ANSI TRIM(BOTH ... FROM ...) form) — the matched
    // key must pick the exact one the arity resolved to, not fall back.
    [Theory]
    [InlineData(1, "2019", true, null)]
    [InlineData(1, "2016", false, "2017")]
    [InlineData(2, "2019", false, "2022")]
    [InlineData(2, "2022", true, null)]
    public void Resolve_ArityBoundAndMemberBound_PickExactMatchedKey(
        int arity, string declared, bool expectedSupported, string? expectedRequired)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "Trim", arity, TargetDbms.SqlServer, EngineVersion.Parse(declared));

        Assert.NotNull(result);
        Assert.Equal(expectedSupported, result!.Value.IsSupported);
        Assert.Equal(expectedRequired, result.Value.RequiredVersion);
    }

    [Fact]
    public void Resolve_MemberOverride_WinsOverAVersionBoundInBothDirections()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_datetrunc"] = "supported",
        });

        DialectSupportResolver.Result? result = DialectSupportResolver.Resolve(
            options, "Datetrunc", arity: null, TargetDbms.SqlServer, EngineVersion.Parse("2019"));

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
        Assert.False(result.Value.IsVersionBound);
    }
}
