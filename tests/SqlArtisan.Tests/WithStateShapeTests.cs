using System.Reflection;
using SqlArtisan.Internal;

namespace SqlArtisan.Tests;

// The API side of the two WITH states (#521): plain WITH opens MERGE, the
// recursive one does not, so WithRecursive(...).MergeInto(...) cannot compile.
public class WithStateShapeTests
{
    [Fact]
    public void PlainWithState_OpensMerge()
    {
        Assert.Contains(typeof(IMergeBuilder), typeof(IWithBuilderWith).GetInterfaces());
    }

    [Fact]
    public void RecursiveWithState_OpensNoMerge()
    {
        Assert.DoesNotContain(
            typeof(IMergeBuilder), typeof(IWithBuilderWithRecursive).GetInterfaces());
        Assert.Null(typeof(IWithBuilderWithRecursive).GetMethod("MergeInto"));
    }

    // Absence is only provable where the factory hands back the narrow state:
    // a WithRecursive returning the plain one would restore the chain.
    [Fact]
    public void WithRecursiveFactory_ReturnsTheRecursiveState()
    {
        MethodInfo? withRecursive = typeof(Sql).GetMethod(
            "WithRecursive", [typeof(CommonTableExpression[])]);

        Assert.NotNull(withRecursive);
        Assert.Equal(typeof(IWithBuilderWithRecursive), withRecursive.ReturnType);
    }

    // Every statement the plain state opens is shared but MERGE, so the split
    // costs the recursive chain nothing else.
    [Fact]
    public void RecursiveWithState_KeepsEveryOtherStatement()
    {
        Type[] recursive = typeof(IWithBuilderWithRecursive).GetInterfaces();

        Assert.Equal(
            [typeof(IDeleteBuilder), typeof(IInsertBuilder), typeof(ISelectBuilder),
                typeof(IUpdateBuilder)],
            recursive.OrderBy(t => t.Name, StringComparer.Ordinal));
    }
}
