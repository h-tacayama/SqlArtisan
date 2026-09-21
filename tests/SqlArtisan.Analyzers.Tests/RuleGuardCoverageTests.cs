using System.Text.RegularExpressions;

namespace SqlArtisan.Analyzers.Tests;

// A shared guard (FluentChain.IsForeignInvocation, the pseudo-row list) is
// invisible to a mutation of one rule unless that rule's own suite exercises it
// (release audit pass 8), so each reading rule carries the silence case.
public class RuleGuardCoverageTests
{
    private static readonly Regex s_descriptor = new(
        @"DiagnosticDescriptors\.(\w+)",
        RegexOptions.Compiled);

    [Fact]
    public void EveryRuleReadingIsForeignInvocation_HasAHelperMethodSilenceTest()
    {
        string root = FindRepoRoot();
        string analyzers = Path.Combine(root, "src", "SqlArtisan.Analyzers");
        string tests = Path.Combine(root, "tests", "SqlArtisan.Analyzers.Tests");
        string descriptors = File.ReadAllText(Path.Combine(analyzers, "DiagnosticDescriptors.cs"));
        List<string> uncovered = [];

        foreach (string rule in Directory.EnumerateFiles(analyzers, "*Rule.cs"))
        {
            string source = File.ReadAllText(rule);
            if (!source.Contains("IsForeignInvocation", StringComparison.Ordinal))
            {
                continue;
            }

            string id = DiagnosticId(descriptors, s_descriptor.Match(source).Groups[1].Value);
            bool covered = Directory.EnumerateFiles(tests, "*AnalyzerTests.cs")
                .Select(File.ReadAllText)
                .Any(test => test.Contains($"\"{id}\"", StringComparison.Ordinal)
                    && test.Contains("FromHelperMethod_Silent", StringComparison.Ordinal));
            if (!covered)
            {
                uncovered.Add($"{Path.GetFileName(rule)} ({id})");
            }
        }

        Assert.True(
            uncovered.Count == 0,
            "rules with no *FromHelperMethod_Silent test: " + string.Join(", ", uncovered));
    }

    [Fact]
    public void EveryPseudoRowReference_IsNamedInTheSqla0204Suite()
    {
        string tests = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "tests",
            "SqlArtisan.Analyzers.Tests",
            "UnusableIndexPredicateAnalyzerTests.cs"));

        foreach (string member in UnusableIndexPredicateRule.PseudoRowReferences)
        {
            Assert.True(
                tests.Contains($"{member}(", StringComparison.Ordinal)
                    || tests.Contains($"\"{member}\"", StringComparison.Ordinal),
                $"{member} is a pseudo-row reference no SQLA0204 test names.");
        }
    }

    private static string DiagnosticId(string descriptors, string field)
    {
        Match match = Regex.Match(descriptors, field + @"\s*=\s*new\(\s*id:\s*""(SQLA\d{4})""");
        Assert.True(match.Success, $"descriptor field {field} not found");
        return match.Groups[1].Value;
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
