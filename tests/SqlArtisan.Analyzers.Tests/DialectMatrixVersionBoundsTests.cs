using System;
using System.Collections.Generic;
using System.IO;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards <see cref="DialectMatrix.AllBounds"/> against drifting out of sync
/// with the plain <see cref="DbmsSupport"/> bool it refines: every bound must
/// key an entry that still exists, and every bound must agree with the bool
/// at the dialect's verification baseline — the fact the two tables are
/// required to tell one consistent story about the same engine version.
/// Failures aggregate (rather than one Theory case per row) so the suite
/// stays green with zero rows before the register is seeded.
/// </summary>
public class DialectMatrixVersionBoundsTests
{
    private static readonly TargetDbms[] AllTargets =
    [
        TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite, TargetDbms.SqlServer,
    ];

    [Fact]
    public void EveryBound_KeysARealMatrixEntry()
    {
        List<string> orphans = [];
        foreach (MatrixKey key in DialectMatrix.AllBounds.Keys)
        {
            bool exists = false;
            foreach (MatrixKey real in DialectMatrix.AllKeys)
            {
                if (real.MemberName == key.MemberName && real.Arity == key.Arity)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                orphans.Add(Label(key));
            }
        }

        Assert.True(orphans.Count == 0, $"Bound(s) with no matching matrix entry: {string.Join(", ", orphans)}");
    }

    [Fact]
    public void EveryBound_AgreesWithEntryBool_AtVerificationBaseline()
    {
        List<string> mismatches = [];
        foreach ((MatrixKey key, VersionBounds bounds) in DialectMatrix.AllBounds)
        {
            if (!DialectMatrix.TryGetEntry(key.MemberName, key.Arity, out DbmsSupport support, out _))
            {
                continue;
            }

            foreach (TargetDbms target in AllTargets)
            {
                if (bounds.MinFor(target) is not { } min)
                {
                    continue;
                }

                bool baselineMeetsBound = DialectMatrix.BaselineVersion[target] >= min;
                if (support.IsSupported(target) != baselineMeetsBound)
                {
                    mismatches.Add(
                        $"{Label(key)}/{target}: entry bool is {support.IsSupported(target)} but baseline "
                            + $"{DialectMatrix.BaselineVersion[target]} vs bound {min} says {baselineMeetsBound}");
                }
            }
        }

        Assert.True(mismatches.Count == 0, $"Bound/bool disagreement(s) at baseline: {string.Join("; ", mismatches)}");
    }

    // Cheap drift guard (DialectMatrixDocsTests's pattern): every seeded bound's
    // construct name and version token must appear somewhere in the provenance
    // table docs/analyzer.md ships alongside SQLA0006 — a renamed/re-bounded
    // row that forgets to update the docs fails here instead of shipping stale.
    [Fact]
    public void EveryBound_HasDocsProvenance()
    {
        string doc = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "analyzer.md"));

        List<string> missing = [];
        foreach ((MatrixKey key, VersionBounds bounds) in DialectMatrix.AllBounds)
        {
            if (!doc.Contains(key.MemberName))
            {
                missing.Add($"{Label(key)}: construct name not found in docs/analyzer.md");
            }

            foreach (TargetDbms target in AllTargets)
            {
                if (bounds.MinFor(target) is not { } min)
                {
                    continue;
                }

                if (!doc.Contains(min.ToString()))
                {
                    missing.Add($"{Label(key)}/{target}: version token \"{min}\" not found in docs/analyzer.md");
                }
            }
        }

        Assert.True(missing.Count == 0, $"Undocumented bound(s): {string.Join("; ", missing)}");
    }

    private static string Label(MatrixKey key) => key.Arity is { } arity ? $"{key.MemberName} (arity {arity})" : key.MemberName;

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
