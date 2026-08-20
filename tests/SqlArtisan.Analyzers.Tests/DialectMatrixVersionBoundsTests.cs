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
///
/// <para>
/// Both directions are gated (#500): the orphan check reads
/// <c>Bounds</c> -&gt; <c>Entries</c>, and the re-key check reads
/// <c>Entries</c> -&gt; <c>Bounds</c>, since an arity entry added beside a
/// member-level bound moves <c>Evaluate</c>'s lookup onto a key
/// <see cref="DialectMatrix.TryGetMinVersion"/> resolves exactly (ADR 0015)
/// and would otherwise drop that overload's version verdict in silence.
/// </para>
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

    // A member-level bound is not inherited: TryGetMinVersion resolves the key
    // Evaluate matched, which is the arity key once an arity entry exists. The
    // floor is required exactly where the arity cell agrees with the member
    // floor's own baseline verdict — the sibling gate's invariant, so a cell
    // that disagrees could not carry the floor anyway. Reading the cell alone
    // would invert above the baseline, where the bound sits on a false cell and
    // it is the *false* arity cell that still needs it (the pgvector rows).
    [Fact]
    public void EveryArityEntry_ReKeysItsMemberLevelBound()
    {
        List<string> dropped = [];
        foreach (MatrixKey key in DialectMatrix.AllKeys)
        {
            if (key.Arity is not { } arity
                || !DialectMatrix.AllBounds.TryGetValue(new MatrixKey(key.MemberName), out VersionBounds memberBound)
                || !DialectMatrix.TryGetEntry(key.MemberName, arity, out DbmsSupport support, out _))
            {
                continue;
            }

            DialectMatrix.AllBounds.TryGetValue(key, out VersionBounds arityBound);

            foreach (TargetDbms target in AllTargets)
            {
                if (memberBound.MinFor(target) is not { } memberMin
                    || support.IsSupported(target) != DialectMatrix.BaselineVersion[target] >= memberMin
                    || arityBound.MinFor(target) is not null)
                {
                    continue;
                }

                dropped.Add($"{Label(key)}/{target}: {Label(new MatrixKey(key.MemberName))} bounds it at {memberMin}");
            }
        }

        Assert.True(
            dropped.Count == 0,
            "Arity entry/entries shadowing a member-level bound with no bound of their own — the "
                + "member-level floor is not inherited, so the overload loses that floor's verdict: a "
                + "missing SQLA0101 below the dialect's baseline, a false-positive SQLA0100 above it. "
                + "Re-key the floor onto the arity key (its own value, where the overload's floor "
                + $"differs — Trim's 2-arg form is SQL Server 2022 where the member is 2017): {string.Join("; ", dropped)}");
    }

    // Drift guard (DialectMatrixDocsTests's pattern): every seeded bound's
    // version token must appear, digit-bounded, on a docs/analyzer.md line that
    // also names the construct. Both halves matter: a whole-file substring
    // check let 25 of 69 bound cells survive a one-step re-bound (common
    // tokens occur in other rows), and an unbounded Contains let a prefix
    // mutation ("8.0.31" -> "8.0") survive on the construct's own line.
    [Fact]
    public void EveryBound_HasDocsProvenance()
    {
        string[] docLines = File.ReadAllLines(Path.Combine(FindRepoRoot(), "docs", "analyzer.md"));

        List<string> missing = [];
        foreach ((MatrixKey key, VersionBounds bounds) in DialectMatrix.AllBounds)
        {
            string[] nameLines = [.. docLines.Where(line => line.Contains($"`{key.MemberName}`"))];

            if (nameLines.Length == 0)
            {
                missing.Add($"{Label(key)}: construct name not found in docs/analyzer.md");
                continue;
            }

            foreach (TargetDbms target in AllTargets)
            {
                if (bounds.MinFor(target) is not { } min)
                {
                    continue;
                }

                System.Text.RegularExpressions.Regex token = new(
                    $@"(?<![\d.]){System.Text.RegularExpressions.Regex.Escape(min.ToString())}(?![\d.])");
                if (!nameLines.Any(line => token.IsMatch(line)))
                {
                    missing.Add(
                        $"{Label(key)}/{target}: version token \"{min}\" not found on any "
                            + $"docs/analyzer.md line naming `{key.MemberName}`");
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
