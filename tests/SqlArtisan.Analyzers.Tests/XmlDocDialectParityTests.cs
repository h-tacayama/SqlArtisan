using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SqlArtisan.Internal;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards the two doc/behaviour drifts fixed in #292 from recurring:
/// (1) an <c>InsertInto(table)</c> XML doc promising a <c>.Select(...)</c>
/// continuation its return type does not expose, and (2) a function's
/// <c>&lt;remarks&gt;</c> dialect note disagreeing with <see cref="DialectMatrix"/>.
/// Scoped to a curated member list on purpose — a whole-API remarks parser would
/// be brittle against free-text prose, so this pins only the members the fix touched.
/// </summary>
public class XmlDocDialectParityTests
{
    // The column-less INSERT builders expose only Set/Values; only the
    // column-list builders (ISelectBuilder) carry Select. #292's first drift
    // was a doc claiming .Select() on the column-less overload.
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

    // Each row: the member's <remarks> dialect note must match the matrix entry
    // for (name, arity). Arity is the declared parameter count the analyzer keys on.
    public static IEnumerable<object[]> RemarkCases() =>
    [
        ["M:SqlArtisan.Sql.ToNumber(System.Object)", "ToNumber", 1],
        ["M:SqlArtisan.Sql.Of(SqlArtisan.DbColumn)", "Of", 1],
        ["P:SqlArtisan.Sql.Nowait", "Nowait", null!],
        ["M:SqlArtisan.Sql.Trim(System.Object,System.Object)", "Trim", 2],
        ["M:SqlArtisan.Sql.Substr(System.Object,System.Object)", "Substr", 2],
        ["M:SqlArtisan.Sql.Rtrim(System.Object,System.Object)", "Rtrim", 2],
        ["M:SqlArtisan.Sql.Ltrim(System.Object,System.Object)", "Ltrim", 2],
        ["M:SqlArtisan.Sql.Rpad(System.Object,System.Object)", "Rpad", 2],
        ["M:SqlArtisan.Sql.Rpad(System.Object,System.Object,System.Object)", "Rpad", 3],
        ["M:SqlArtisan.Sql.Lpad(System.Object,System.Object)", "Lpad", 2],
        ["M:SqlArtisan.Sql.Lpad(System.Object,System.Object,System.Object)", "Lpad", 3],
    ];

    [Theory]
    [MemberData(nameof(RemarkCases))]
    public void RemarksDialectNote_MatchesMatrix(string memberId, string name, int? arity)
    {
        string remark = ReadRemark(memberId);
        ISet<TargetDbms> claimed = ParseDialects(remark);

        bool found = DialectMatrix.TryGetEntry(name, arity, out DbmsSupport support, out _);
        Assert.True(found, $"No DialectMatrix entry for {name}/{arity?.ToString() ?? "member"}.");

        foreach (TargetDbms dbms in AllDbms)
        {
            Assert.True(
                claimed.Contains(dbms) == support.IsSupported(dbms),
                $"{memberId} remark \"{remark}\" disagrees with the matrix on {dbms} "
                    + $"(remark says {(claimed.Contains(dbms) ? "supported" : "unsupported")}, "
                    + $"matrix says {(support.IsSupported(dbms) ? "supported" : "unsupported")}).");
        }
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

    // Both house phrasings: "Not supported by X." = all but X; "A, B, and C
    // syntax." = exactly the named set. Substring checks are unambiguous because
    // the five display names do not contain one another.
    private static ISet<TargetDbms> ParseDialects(string remark)
    {
        IEnumerable<TargetDbms> named = DisplayNames
            .Where(pair => remark.Contains(pair.Key))
            .Select(pair => pair.Value);

        return remark.Contains("Not supported by")
            ? AllDbms.Except(named).ToHashSet()
            : [.. named];
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
