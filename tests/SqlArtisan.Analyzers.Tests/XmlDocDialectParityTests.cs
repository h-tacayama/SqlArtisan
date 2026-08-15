using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SqlArtisan.Internal;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards #292's doc/behaviour drifts from recurring.
/// <see cref="RemarksDialectNote_MatchesMatrix"/> sweeps every public member
/// carrying both a <c>&lt;remarks&gt;</c> dialect note and a
/// <see cref="DialectMatrix"/> entry against a documented exclusion catalog
/// (#470), rather than a curated inclusion list.
/// </summary>
public class XmlDocDialectParityTests
{
    // #292: the doc promised .Select() here, but only ISelectBuilder-backed
    // column-list builders carry it.
    [Fact]
    public void ColumnlessInsertBuilders_HaveNoSelectContinuation()
    {
        Assert.False(HasSelect(typeof(IInsertBuilderTable)));
        Assert.False(HasSelect(typeof(IInsertIgnoreBuilderTable)));
    }

    [Fact]
    public void ColumnListInsertBuilders_HaveSelectContinuation()
    {
        Assert.True(HasSelect(typeof(IInsertBuilderColumns)));
        Assert.True(HasSelect(typeof(IInsertIgnoreBuilderColumns)));
    }

    // Members whose first-clause parse disagrees with the matrix — the remark's
    // phrasing defeats the parser, not its content, so it is catalogued rather
    // than gated. ExcludedMembers_AreAllLoadBearing enforces exactly that much.
    private static readonly IReadOnlySet<(string Name, int? Arity)> ExcludedMembers = new HashSet<(string, int?)>
    {
        // These four can never be retired by rewording: DialectMatrix keys them
        // as the *union* of two distinct APIs colliding on one (name, arity)
        // (see its own doc comment), which no single remark can represent.
        ("Match", 2),
        ("Nextval", 1),
        ("Currval", 1),
        ("GroupConcat", 2),

        ("Concat", 2),
        ("Date", 1),
        ("Datediff", 3),
        ("Exp", 1),
        ("If", 3),
        ("IntervalLiteral", 2),
    };

    // Every member whose <remarks> names a dialect — i.e. contains one of
    // DisplayNames' keys — and has a DialectMatrix entry. A remark that never
    // names a dialect (implementation notes, usage guidance) makes no
    // matrix-parity claim to check, even when the member's matrix entry happens
    // to be <see cref="DbmsSupport.All"/>.
    private static IEnumerable<(string Id, string Name, int? Arity)> Candidates() =>
        from member in LoadXmlDoc().Descendants("member")
        let remark = member.Element("remarks")
        where remark is not null
        // Collapsed before the name match for the same reason ParseDialects
        // collapses: a doc comment wraps, so "SQL Server" can straddle a line
        // break and would otherwise read as naming no dialect at all (#469).
        let text = WhitespaceRun.Replace(remark.Value, " ")
        where DisplayNames.Keys.Any(text.Contains)
        let id = (string)member.Attribute("name")!
        let parsed = ParseMemberId(id)
        where DialectMatrix.TryGetEntry(parsed.Name, parsed.Arity, out _, out _)
        select (id, parsed.Name, parsed.Arity);

    public static IEnumerable<object[]> RemarkCases() =>
        from candidate in Candidates()
        where !ExcludedMembers.Contains((candidate.Name, candidate.Arity))
        select new object[] { candidate.Id, candidate.Name, candidate.Arity! };

    // An exclusion that suppresses nothing is worse than none: it reads as
    // load-bearing and keeps its member out of the sweep for good. The repo's
    // other documented-exclusion catalogs carry the same staleness gate —
    // DialectMatrixCoverageTests' Exclusions_ResolveToRealMembersWithoutMatrixEntries
    // and MatrixSweepTests' Catalog_HasNoKeysOutsideTheMatrix.
    [Fact]
    public void ExcludedMembers_AreAllLoadBearing()
    {
        ILookup<(string, int?), (string Id, string Name, int? Arity)> byKey =
            Candidates().ToLookup(candidate => (candidate.Name, candidate.Arity));

        // An empty key — member gone, remark gone, or it no longer names a
        // dialect — vacuously satisfies All and so reads as inert too.
        List<string> inert = [.. ExcludedMembers
            .Where(entry => byKey[entry].All(ParsesToMatrixSet))
            .Select(entry => $"{entry.Name}/{entry.Arity?.ToString() ?? "member"}")
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.True(
            inert.Count == 0,
            $"{inert.Count} exclusions suppress nothing — the remark now parses to the matrix set, "
                + $"or the member no longer reaches the sweep at all — so retire them:\n  "
                + string.Join("\n  ", inert));
    }

    // An empty parse is a parser gap, not agreement: the sweep rejects it
    // outright, so the exclusion is still carrying that member.
    private static bool ParsesToMatrixSet((string Id, string Name, int? Arity) candidate)
    {
        ISet<TargetDbms> claimed = ParseDialects(ReadRemark(candidate.Id));
        ISet<TargetDbms> matrixSet = SupportedDialects(candidate.Name, candidate.Arity);

        return claimed.Count > 0 && claimed.SetEquals(matrixSet);
    }

    [Theory]
    [MemberData(nameof(RemarkCases))]
    public void RemarksDialectNote_MatchesMatrix(string memberId, string name, int? arity)
    {
        string remark = ReadRemark(memberId);
        ISet<TargetDbms> claimed = ParseDialects(remark);

        Assert.True(claimed.Count > 0,
            $"{memberId} remark \"{remark}\" named no dialect — likely a parser gap, not a "
                + "genuine claim of universal (non-)support. Add it to ExcludedMembers if the "
                + "remark's shape is legitimately unparseable.");

        ISet<TargetDbms> support = SupportedDialects(name, arity);

        foreach (TargetDbms dbms in AllDbms)
        {
            Assert.True(
                claimed.Contains(dbms) == support.Contains(dbms),
                $"{memberId} remark \"{remark}\" disagrees with the matrix on {dbms} "
                    + $"(remark says {(claimed.Contains(dbms) ? "supported" : "unsupported")}, "
                    + $"matrix says {(support.Contains(dbms) ? "supported" : "unsupported")}).");
        }
    }

    // A dialect carrying a version bound is supported by some version of it, so a
    // remark naming it — floor or not — agrees with the matrix. The analyzer only
    // reaches that verdict once a target declares a version
    // (DialectSupportResolver.Evaluate); under `any` it falls back to the plain
    // bool. TryGetMinVersion is an exact key lookup, so the matched key must match.
    private static ISet<TargetDbms> SupportedDialects(string name, int? arity)
    {
        bool found = DialectMatrix.TryGetEntry(name, arity, out DbmsSupport support, out bool wasArityMatch);
        Assert.True(found, $"No DialectMatrix entry for {name}/{arity?.ToString() ?? "member"}.");

        MatrixKey matchedKey = new(name, wasArityMatch ? arity : null);
        return new HashSet<TargetDbms>(AllDbms.Where(
            dbms => support.IsSupported(dbms) || DialectMatrix.TryGetMinVersion(matchedKey, dbms, out _)));
    }

    private static readonly TargetDbms[] AllDbms =
        [TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite, TargetDbms.SqlServer];

    private static readonly IReadOnlyDictionary<string, TargetDbms> DisplayNames = new Dictionary<string, TargetDbms>
    {
        ["MySQL"] = TargetDbms.MySql,
        ["Oracle"] = TargetDbms.Oracle,
        ["PostgreSQL"] = TargetDbms.PostgreSql,
        ["SQLite"] = TargetDbms.Sqlite,
        ["SQL Server"] = TargetDbms.SqlServer,
    };

    // "Not supported by X." / "Not available on X." = all but X; "A, B, and C
    // syntax." = exactly the named set — a remark uses one form or the other,
    // never both. Both branches read only their own clause, so a dialect named
    // elsewhere in the remark — a version floor (#469), a cross-reference to a
    // sibling factory — is not swept into either set.
    private static ISet<TargetDbms> ParseDialects(string remark)
    {
        // A doc comment wraps, so a display name can straddle a line break.
        string text = WhitespaceRun.Replace(remark, " ");
        Match exclusion = ExclusionClause.Match(text);

        return exclusion.Success
            ? AllDbms.Except(NamedIn(exclusion.Groups[1].Value)).ToHashSet()
            : [.. NamedIn(SupportedClause.Match(text).Value)];
    }

    // Substring match is safe: no display name contains another.
    private static IEnumerable<TargetDbms> NamedIn(string text) =>
        DisplayNames.Where(pair => text.Contains(pair.Key)).Select(pair => pair.Value);

    private static readonly Regex WhitespaceRun = new(@"\s+");

    // The clause ends at the first sentence/sub-clause boundary — a period not
    // inside a version number (".0") or a "Foo(...)" ellipsis, or a semicolon —
    // or an em dash, defensively bounding a "— use <sibling> there" aside, which
    // names a function, not a dialect.
    private const string ClauseBody = @"(?:\.\.\.|\.\d|[^.;—])*";
    private static readonly Regex SupportedClause = new(ClauseBody);
    private static readonly Regex ExclusionClause = new($@"Not (?:supported by|available on)({ClauseBody})");

    // memberId is "M:"/"P:"/"T:" plus a dotted signature, e.g.
    // "M:SqlArtisan.Sql.ToNumber(System.Object,System.Object)" or
    // "P:SqlArtisan.Sql.Nowait". Arity is the declared parameter count the
    // analyzer keys on — null for a property.
    private static (string Name, int? Arity) ParseMemberId(string memberId)
    {
        string signature = memberId[2..];
        int parenIndex = signature.IndexOf('(');
        if (parenIndex < 0)
        {
            return (signature[(signature.LastIndexOf('.') + 1)..], null);
        }

        string beforeParen = signature[..parenIndex];
        string name = beforeParen[(beforeParen.LastIndexOf('.') + 1)..];

        string parameters = signature[(parenIndex + 1)..^1];
        if (parameters.Length == 0)
        {
            return (name, 0);
        }

        int depth = 0;
        int arity = 1;
        foreach (char c in parameters)
        {
            if (c is '{' or '(')
            {
                depth++;
            }
            else if (c is '}' or ')')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                arity++;
            }
        }

        return (name, arity);
    }

    private static bool HasSelect(Type type) =>
        type.GetInterfaces().Prepend(type)
            .SelectMany(t => t.GetMethods())
            .Any(m => m.Name == "Select");

    private static string ReadRemark(string memberId)
    {
        XElement? remark = LoadXmlDoc()
            .Descendants("member")
            .First(m => (string?)m.Attribute("name") == memberId)
            .Element("remarks");

        Assert.NotNull(remark);
        return remark!.Value.Trim();
    }

    private static XDocument LoadXmlDoc()
    {
        string xmlPath = Path.Combine(
            Path.GetDirectoryName(typeof(Sql).Assembly.Location)!, "SqlArtisan.xml");
        return XDocument.Load(xmlPath);
    }
}
