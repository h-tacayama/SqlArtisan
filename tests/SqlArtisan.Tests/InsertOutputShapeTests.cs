using SqlArtisan.Internal;

namespace SqlArtisan.Tests;

// The API side of the columnless INSERT's OUTPUT (#521): the state after OUTPUT
// withholds Set(...), whose column list T-SQL puts ahead of OUTPUT.
public class InsertOutputShapeTests
{
    [Fact]
    public void ColumnlessInsertState_OffersOutput()
    {
        Assert.NotNull(typeof(IInsertBuilderTableOutput).GetMethod("Output"));
        Assert.Contains(
            typeof(IInsertBuilderTable), typeof(IInsertBuilderTableOutput).GetInterfaces());
    }

    [Fact]
    public void StateAfterColumnlessOutput_OffersNoSet()
    {
        Assert.Null(typeof(IInsertBuilderTableOutputInto).GetMethod("Set"));
        Assert.Null(typeof(IInsertBuilderTableOutputRows).GetMethod("Set"));
        Assert.DoesNotContain(
            typeof(IInsertBuilderTable),
            typeof(IInsertBuilderTableOutputInto).GetInterfaces());
    }

    // The other tests here inspect the interfaces alone; if Sql.InsertInto handed back
    // the old state, Output(...) would be declared yet unreachable from it.
    [Fact]
    public void ColumnlessInsertFactory_ReturnsTheOutputCapableState()
    {
        Assert.Equal(
            typeof(IInsertBuilderTableOutput),
            typeof(Sql).GetMethod("InsertInto", [typeof(DbTableBase)])!.ReturnType);
    }

    [Fact]
    public void StateAfterColumnlessOutputInto_OffersNoSecondInto()
    {
        Assert.Null(typeof(IInsertBuilderTableOutputRows).GetMethod("Into"));
    }
}
