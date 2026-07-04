using System.Collections.Generic;

namespace SqlArtisan.Analyzers.Tests;

public class DialectMatrixTests
{
    [Fact]
    public void TryGetEntryFrom_ArityEntryPresent_WinsOverMemberEntry()
    {
        // Synthetic data only — real dialect facts live in DialectMatrix.Entries and must be
        // primary-source-verified; this proves the arity-priority lookup itself, nothing about SQL.
        var entries = new Dictionary<MatrixKey, DbmsSupport>
        {
            [new MatrixKey("Synthetic")] = DbmsSupport.All,
            [new MatrixKey("Synthetic", 2)] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        };

        bool found = DialectMatrix.TryGetEntryFrom(entries, "Synthetic", arity: 2, out DbmsSupport support, out bool wasArityMatch);

        Assert.True(found);
        Assert.True(wasArityMatch);
        Assert.False(support.MySql);
    }

    [Fact]
    public void TryGetEntryFrom_NoArityEntry_FallsBackToMemberEntry()
    {
        var entries = new Dictionary<MatrixKey, DbmsSupport>
        {
            [new MatrixKey("Synthetic")] = DbmsSupport.All,
        };

        bool found = DialectMatrix.TryGetEntryFrom(entries, "Synthetic", arity: 3, out DbmsSupport support, out bool wasArityMatch);

        Assert.True(found);
        Assert.False(wasArityMatch);
        Assert.True(support.MySql);
    }

    [Fact]
    public void TryGetEntryFrom_UnknownMember_ReturnsFalse()
    {
        var entries = new Dictionary<MatrixKey, DbmsSupport>();

        bool found = DialectMatrix.TryGetEntryFrom(entries, "Unknown", arity: null, out _, out _);

        Assert.False(found);
    }

    [Fact]
    public void Entries_RollupOnMySql_IsUnsupported()
    {
        Assert.True(DialectMatrix.TryGetEntry("Rollup", arity: null, out DbmsSupport support, out _));
        Assert.False(support.IsSupported(TargetDbms.MySql));
        Assert.True(support.IsSupported(TargetDbms.Oracle));
    }
}
