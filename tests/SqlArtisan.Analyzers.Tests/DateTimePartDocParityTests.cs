using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SqlArtisan.Analyzers;
using Xunit;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// <c>DateTimePart</c>'s member summaries attribute each part to the dialects
/// whose grammar takes it, with <see cref="DatepartValidity"/> as the source of
/// truth. <c>XmlDocDialectParityTests</c> reads neither surface (#523).
/// </summary>
public class DateTimePartDocParityTests
{
    // Every attribution parses today, so nothing is catalogued. The entry shape
    // and the staleness gate below exist for the phrasing that eventually
    // defeats the parser, as XmlDocDialectParityTests' own catalog does.
    private static readonly IReadOnlySet<string> ExcludedMembers = new HashSet<string>();

    private static readonly IReadOnlyDictionary<string, TargetDbms> DisplayNames =
        new Dictionary<string, TargetDbms>
        {
            ["MySQL"] = TargetDbms.MySql,
            ["Oracle"] = TargetDbms.Oracle,
            ["PostgreSQL"] = TargetDbms.PostgreSql,
            ["SQLite"] = TargetDbms.Sqlite,
            ["SQL Server"] = TargetDbms.SqlServer,
        };

    private static readonly Regex WhitespaceRun = new(@"\s+");

    // The attribution is the parenthesized aside's first clause. A dialect named
    // past the first ';' is a cross-reference — Dayofyear's "PostgreSQL uses
    // Doy" points away from PostgreSQL rather than claiming it.
    private static readonly Regex AttributionAside = new(@"\(([^();]*)");

    [Theory]
    [MemberData(nameof(AttributedMembers))]
    public void EveryAttributedMember_NamesOnlyDialectsThatTakeThePart(string member)
    {
        HashSet<TargetDbms> claimed = NamedDialects(SummaryOf(member));

        List<string> wrong = [.. claimed
            .Where(dbms => !PartsTakenBy(dbms).Contains(member))
            .Select(dbms => DisplayNames.First(pair => pair.Value == dbms).Key)
            .OrderBy(name => name, StringComparer.Ordinal)];

        Assert.True(
            wrong.Count == 0,
            $"DateTimePart.{member}'s summary attributes the part to "
                + $"{string.Join(", ", wrong)}, which DatepartValidity's list(s) for "
                + "that dialect do not accept — correct the summary or the list.");
    }

    // An exclusion that suppresses nothing reads as load-bearing and keeps its
    // member out of the sweep for good; the repo's other catalogs gate the same way.
    [Fact]
    public void ExcludedMembers_AreAllLoadBearing()
    {
        List<string> inert = [.. ExcludedMembers
            .Where(member => !Candidates().Contains(member)
                || NamedDialects(SummaryOf(member))
                    .All(dbms => PartsTakenBy(dbms).Contains(member)))
            .OrderBy(member => member, StringComparer.Ordinal)];

        Assert.True(
            inert.Count == 0,
            $"{inert.Count} exclusion(s) suppress nothing — the summary now agrees with "
                + $"DatepartValidity, or the member no longer reaches the sweep — so retire "
                + $"them:\n  {string.Join("\n  ", inert)}");
    }

    // A part no list accepts is unreachable for SQLA0104 and its summary's
    // attribution answers to nothing, so the sweep must still see the member.
    [Fact]
    public void EveryAttributedMember_IsKnownToDatepartValidity()
    {
        List<string> unknown = [.. Candidates()
            .Where(member => !DatepartValidity.AllKnownDatepartNames.Contains(member))
            .OrderBy(member => member, StringComparer.Ordinal)];

        Assert.True(
            unknown.Count == 0,
            $"{unknown.Count} attributed DateTimePart member(s) appear in no DatepartValidity "
                + $"list at all:\n  {string.Join("\n  ", unknown)}");
    }

    public static TheoryData<string> AttributedMembers()
    {
        TheoryData<string> data = [];
        foreach (string member in Candidates().Where(m => !ExcludedMembers.Contains(m)))
        {
            data.Add(member);
        }

        return data;
    }

    private static IEnumerable<string> Candidates() =>
        Enum.GetNames(typeof(DateTimePart))
            .Where(member => NamedDialects(SummaryOf(member)).Count > 0);

    private static HashSet<TargetDbms> NamedDialects(string summary)
    {
        Match aside = AttributionAside.Match(WhitespaceRun.Replace(summary, " "));

        return aside.Success
            ? [.. DisplayNames.Where(pair => aside.Groups[1].Value.Contains(pair.Key))
                .Select(pair => pair.Value)]
            : [];
    }

    private static HashSet<string> PartsTakenBy(TargetDbms dbms)
    {
        HashSet<string> parts = new(StringComparer.Ordinal);
        foreach (string factory in DatepartValidity.DatepartParameterName.Keys)
        {
            parts.UnionWith(DatepartValidity.For(factory, dbms) ?? []);
        }

        return parts;
    }

    private static string SummaryOf(string member) =>
        LoadXmlDoc()
            .Descendants("member")
            .FirstOrDefault(m =>
                (string?)m.Attribute("name") == $"F:SqlArtisan.DateTimePart.{member}")
            ?.Element("summary")?.Value
        ?? string.Empty;

    private static XDocument LoadXmlDoc() => XDocument.Load(Path.Combine(
        Path.GetDirectoryName(typeof(Sql).Assembly.Location)!, "SqlArtisan.xml"));
}
