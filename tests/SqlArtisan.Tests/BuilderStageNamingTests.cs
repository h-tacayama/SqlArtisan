using System.Reflection;
using System.Text.RegularExpressions;

namespace SqlArtisan.Tests;

// Pins the shape of public-api-design.md's stage-name clause (#568) over every
// Internal interface a public member returns, plus the named entries — so a
// stage cannot drop its prefix, with or without Builder. The state word is review's.
public class BuilderStageNamingTests
{
    private static readonly Regex s_stageName = new(
        "^I(Select|With|Insert|InsertIgnore|Update|Delete|Merge|Returning)"
            + "Builder([A-Z][A-Za-z]*)?$",
        RegexOptions.Compiled);

    // A capability that also ends an upsert action (DoNothing(), DoUpdateSet().Where()).
    private static readonly HashSet<string> s_capabilityStages = ["IReturning"];

    public static IEnumerable<object[]> StageInterfaces()
    {
        Type[] exported = typeof(ISqlBuilder).Assembly.GetExportedTypes();

        IEnumerable<Type> returned = exported
            .SelectMany(t => t.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(m => m.ReturnType);
        IEnumerable<Type> named = exported
            .Where(t => t.Name.Contains("Builder", StringComparison.Ordinal));

        return returned
            .Concat(named)
            .Where(t => t.IsInterface && t.Namespace == "SqlArtisan.Internal")
            .Select(t => t.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new object[] { name });
    }

    [Theory]
    [MemberData(nameof(StageInterfaces))]
    public void StageInterface_IsNamedStatementBuilderState(string name)
    {
        if (!s_capabilityStages.Contains(name))
        {
            Assert.Matches(s_stageName, name);
        }
    }

    [Fact]
    public void StageInterfaces_IncludeReturnedAndEntryStages()
    {
        List<string> names = [.. StageInterfaces().Select(row => (string)row[0])];

        Assert.Contains("ISelectBuilderLimitOffset", names);
        Assert.Contains("IDeleteBuilder", names);
        Assert.Contains("IReturning", names);
    }
}
