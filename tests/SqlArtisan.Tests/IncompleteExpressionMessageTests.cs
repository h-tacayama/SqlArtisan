using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// A "pending" expression (a window function before .Over(...), an ordered-set
// aggregate before .WithinGroup(...)) reaching a value position throws with an
// actionable completion hint, consistently across every value position. (#190)
public class IncompleteExpressionMessageTests
{
    private readonly TestTable _t = new("t");

    private const string AgainstHint = "Complete it with .Against(...) or .AgainstScore(...)";
    private const string OverHint = "Complete it with .Over(...)";
    private const string WithinGroupHint = "Complete it with .WithinGroup(OrderBy(...))";

    [Fact]
    public void Rank_WithoutOver_Select_ThrowsWithOverHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(Rank()).From(_t).Build());

        Assert.Contains("is not a complete SQL expression", ex.Message);
        Assert.Contains(OverHint, ex.Message);
    }

    [Fact]
    public void Listagg_WithoutWithinGroup_Select_ThrowsWithWithinGroupHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(Listagg(_t.Name, ", ")).From(_t).Build());

        Assert.Contains("is not a complete SQL expression", ex.Message);
        Assert.Contains(WithinGroupHint, ex.Message);
    }

    [Fact]
    public void PercentileCont_WithoutWithinGroup_Select_ThrowsWithWithinGroupHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(PercentileCont(0.5)).From(_t).Build());

        Assert.Contains(WithinGroupHint, ex.Message);
    }

    [Fact]
    public void PercentileDisc_WithoutWithinGroup_Select_ThrowsWithWithinGroupHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(PercentileDisc(0.5)).From(_t).Build());

        Assert.Contains(WithinGroupHint, ex.Message);
    }

    [Fact]
    public void Match_WithoutAgainst_Select_ThrowsWithAgainstHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(Match(_t.Name)).From(_t).Build());

        Assert.Contains("is not a complete SQL expression", ex.Message);
        Assert.Contains(AgainstHint, ex.Message);
    }

    [Fact]
    public void Rank_WithoutOver_OrderBy_ThrowsWithOverHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(_t.Name).From(_t).OrderBy(Rank()).Build());

        Assert.Contains(OverHint, ex.Message);
    }

    [Fact]
    public void Listagg_WithoutWithinGroup_GroupBy_ThrowsWithWithinGroupHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(_t.Name).From(_t).GroupBy(Listagg(_t.Name, ", ")).Build());

        Assert.Contains(WithinGroupHint, ex.Message);
    }

    [Fact]
    public void Rank_WithoutOver_Where_ThrowsWithOverHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(_t.Name).From(_t).Where(_t.Name == Rank()).Build());

        Assert.Contains(OverHint, ex.Message);
    }

    [Fact]
    public void Rank_WithoutOver_InsertValue_ThrowsWithOverHint()
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => InsertInto(_t).Values(Rank()).Build());

        Assert.Contains(OverHint, ex.Message);
    }

    [Fact]
    public void InvalidType_Select_ThrowsGenericMessage()
    {
        // A genuinely unsupported type still gets the resolver's generic message,
        // not the completion-hint path — the new branch is additive.
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => Select(new object()).From(_t).Build());

        Assert.Contains("Invalid type for SelectItem", ex.Message);
        Assert.DoesNotContain("is not a complete SQL expression", ex.Message);
    }
}
