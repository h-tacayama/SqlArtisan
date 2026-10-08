using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

public class ColumnIndexInfoTests
{
    [Fact]
    public void IsIndexed_PlainLead_ReturnsTrue() =>
        Assert.True(Info(leading: ["name"]).IsIndexed("name"));

    [Fact]
    public void IsIndexed_NoLead_ReturnsFalse() =>
        Assert.False(Info(leading: ["name"]).IsIndexed("age"));

    // A trigram GIN index beside a B-tree one serves `name LIKE '%x%'`, which
    // `true` would have SQLA0204 report.
    [Fact]
    public void IsIndexed_PlainLeadAlsoOtherMethodLead_ReturnsNull() =>
        Assert.Null(Info(leading: ["name"], otherMethod: ["name"]).IsIndexed("name"));

    [Fact]
    public void IsIndexed_PlainLeadAlsoInExpression_ReturnsNull() =>
        Assert.Null(Info(leading: ["name"], expressions: ["upper(name)"]).IsIndexed("name"));

    // A partial B-tree index serves no wrapped predicate either, so it takes nothing
    // from the full index's claim.
    [Fact]
    public void IsIndexed_PlainLeadAlsoPartialLead_ReturnsTrue() =>
        Assert.True(Info(leading: ["name"], partial: ["name"]).IsIndexed("name"));

    [Fact]
    public void IsIndexed_PartialLeadOnly_ReturnsNull() =>
        Assert.Null(Info(partial: ["name"]).IsIndexed("name"));

    private static ColumnIndexInfo Info(
        string[]? leading = null,
        string[]? expressions = null,
        string[]? partial = null,
        string[]? otherMethod = null) =>
        new(leading ?? [], expressions ?? [], partial ?? [], otherMethod ?? []);
}
