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
}
