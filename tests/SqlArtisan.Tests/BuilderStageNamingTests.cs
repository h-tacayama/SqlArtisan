using System.Reflection;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Pins the shape of public-api-design.md's stage-name clause (#568): a stage is
// I<Statement>Builder<State>, never <State>Builder. Which State word fits is
// review's call; this only stops a stage from dropping its statement prefix.
public class BuilderStageNamingTests
{
    private static readonly Regex s_stageName = new(
        "^I(Select|With|Insert|InsertIgnore|Update|Delete|Merge|Returning)"
            + "Builder([A-Z][A-Za-z]*)?$",
        RegexOptions.Compiled);

    public static IEnumerable<object[]> StageInterfaces() =>
        typeof(ISqlBuilder).Assembly.GetExportedTypes()
            .Where(t => t.IsInterface
                && t.Namespace == "SqlArtisan.Internal"
                && t.Name.Contains("Builder", StringComparison.Ordinal))
            .Select(t => t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(StageInterfaces))]
    public void StageInterface_IsNamedStatementBuilderState(string name)
    {
        Assert.Matches(s_stageName, name);
    }

    [Fact]
    public void StageInterfaces_AreFound()
    {
        Assert.Contains(StageInterfaces(), row => (string)row[0] == "ISelectBuilderLimitOffset");
    }
}
