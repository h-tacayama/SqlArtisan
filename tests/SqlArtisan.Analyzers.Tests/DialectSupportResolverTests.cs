using System.Collections.Generic;

namespace SqlArtisan.Analyzers.Tests;

public class DialectSupportResolverTests
{
    [Fact]
    public void ResolveOverride_NoOverrideSet_ReturnsNull()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        DialectSupportResolver.OverrideResult? result = DialectSupportResolver.ResolveOverride(options, "Rollup", arity: null);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveOverride_MemberOverrideSupported_ReturnsSupported()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_rollup"] = "supported",
        });

        DialectSupportResolver.OverrideResult? result = DialectSupportResolver.ResolveOverride(options, "Rollup", arity: null);

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
        Assert.False(result.Value.IsArityLevel);
    }

    [Fact]
    public void ResolveOverride_ArityOverride_WinsOverMemberOverride()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_synthetic"] = "unsupported",
            ["sqlartisan_construct_synthetic_arity2"] = "supported",
        });

        DialectSupportResolver.OverrideResult? oneArg = DialectSupportResolver.ResolveOverride(options, "Synthetic", arity: 1);
        DialectSupportResolver.OverrideResult? twoArg = DialectSupportResolver.ResolveOverride(options, "Synthetic", arity: 2);

        Assert.False(oneArg!.Value.IsSupported);
        Assert.False(oneArg.Value.IsArityLevel);

        Assert.True(twoArg!.Value.IsSupported);
        Assert.True(twoArg.Value.IsArityLevel);
    }

    [Fact]
    public void ResolveOverride_PropertyReferenceHasNoArity_MemberOverrideStillApplies()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["sqlartisan_construct_dual"] = "supported",
        });

        DialectSupportResolver.OverrideResult? result = DialectSupportResolver.ResolveOverride(options, "Dual", arity: null);

        Assert.NotNull(result);
        Assert.True(result!.Value.IsSupported);
    }

    [Fact]
    public void MatchMatrixEntry_UnknownMember_ReturnsNull()
    {
        DialectSupportResolver.MatrixMatch? match = DialectSupportResolver.MatchMatrixEntry("NotInMatrix", arity: null);

        Assert.Null(match);
    }

    [Fact]
    public void MatchMatrixEntry_RealEntry_CarriesTheMemberLevelOverrideHint()
    {
        DialectSupportResolver.MatrixMatch? match = DialectSupportResolver.MatchMatrixEntry("Rollup", arity: null);

        Assert.NotNull(match);
        Assert.Equal("sqlartisan_construct_rollup", match!.Value.OverrideKeyHint);
        Assert.False(match.Value.IsArityLevel);
    }

    [Fact]
    public void Evaluate_NoDeclaredVersion_UsesPlainMatrixBool()
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("Rollup", arity: null)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.MySql, targetVersion: null);

        Assert.False(verdict.IsSupported);
        Assert.False(verdict.IsVersionBound);
    }

    // Rollup has no version bound recorded (DialectMatrix.AllBounds) — declaring
    // a target version must not change its verdict from the plain matrix bool.
    [Fact]
    public void Evaluate_DeclaredVersionEntryHasNoBound_FallsBackToMatrixBool()
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("Rollup", arity: null)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.MySql, EngineVersion.Parse("8.0.16"));

        Assert.False(verdict.IsSupported);
        Assert.False(verdict.IsVersionBound);
        Assert.Null(verdict.RequiredVersion);
    }

    // WithRecursive's Oracle false cell carries no bound (the Oracle-23 candidate
    // was disproven live, #263), so a declared version keeps the plain-bool verdict.
    // The flip direction — a false cell whose bound is met — is covered by the
    // #343 Oracle 23 vector rows and their 23ai live-proof lane.
    [Fact]
    public void Evaluate_FalseCellWithDeclaredVersionAndNoBound_StaysUnsupported()
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("WithRecursive", arity: null)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.Oracle, EngineVersion.Parse("23"));

        Assert.False(verdict.IsSupported);
        Assert.False(verdict.IsVersionBound);
    }

    // WithRecursive is mysql:true in the plain matrix but bound to 8.0 (no CTE
    // support before it) — a declared version below the bound must report the
    // shortfall, not fall back to the (also-true) plain bool.
    [Fact]
    public void Evaluate_TrueCellWithDeclaredVersionBelowBound_ReportsVersionBoundForMySql()
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("WithRecursive", arity: null)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.MySql, EngineVersion.Parse("5.7"));

        Assert.False(verdict.IsSupported);
        Assert.True(verdict.IsVersionBound);
        Assert.Equal("8.0", verdict.RequiredVersion);
    }

    // Datetrunc is sqlServer:true in the plain matrix but bound to 2022 — a
    // declared version below the bound must report SQLA0101, not silence.
    [Fact]
    public void Evaluate_TrueCellWithDeclaredVersionBelowBound_ReportsVersionBound()
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("Datetrunc", arity: null)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.SqlServer, EngineVersion.Parse("2019"));

        Assert.False(verdict.IsSupported);
        Assert.True(verdict.IsVersionBound);
        Assert.Equal("2022", verdict.RequiredVersion);
    }

    // Trim has both a member-level bound (2017, the 1-arg form) and a narrower
    // arity-2 bound (2022, the ANSI TRIM(BOTH ... FROM ...) form) — the matched
    // key must pick the exact one the arity resolved to, not fall back.
    [Theory]
    [InlineData(1, "2019", true, null)]
    [InlineData(1, "2016", false, "2017")]
    [InlineData(2, "2019", false, "2022")]
    [InlineData(2, "2022", true, null)]
    public void Evaluate_ArityBoundAndMemberBound_PicksExactMatchedKey(
        int arity, string declared, bool expectedSupported, string? expectedRequired)
    {
        DialectSupportResolver.MatrixMatch match = DialectSupportResolver.MatchMatrixEntry("Trim", arity)!.Value;

        DialectSupportResolver.MatrixVerdict verdict = DialectSupportResolver.Evaluate(match, TargetDbms.SqlServer, EngineVersion.Parse(declared));

        Assert.Equal(expectedSupported, verdict.IsSupported);
        Assert.Equal(expectedRequired, verdict.RequiredVersion);
    }
}
