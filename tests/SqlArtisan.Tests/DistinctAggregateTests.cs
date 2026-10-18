using System.Reflection;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// COUNT/SUM/AVG(DISTINCT ...) offer only the windows an engine runs on one (#662).
public class DistinctAggregateTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void CountDistinct_Oracle_OverEmpty_CorrectSql()
    {
        SqlStatement sql = Select(Count(Distinct, _t.Code).Over()).Build(Dbms.Oracle);

        Assert.Equal("SELECT COUNT(DISTINCT code) OVER ()", sql.Text);
    }

    [Fact]
    public void SumDistinct_Oracle_OverPartitionBy_CorrectSql()
    {
        SqlStatement sql =
            Select(Sum(Distinct, _t.Code).Over(PartitionBy(_t.Name))).Build(Dbms.Oracle);

        Assert.Equal("SELECT SUM(DISTINCT code) OVER (PARTITION BY name)", sql.Text);
    }

    [Fact]
    public void AvgDistinct_Oracle_OverPartitionBy_CorrectSql()
    {
        SqlStatement sql =
            Select(Avg(Distinct, _t.Code).Over(PartitionBy(_t.Name))).Build(Dbms.Oracle);

        Assert.Equal("SELECT AVG(DISTINCT code) OVER (PARTITION BY name)", sql.Text);
    }

    [Fact]
    public void AvgDistinct_Aliased_CorrectSql()
    {
        SqlStatement sql = Select(Avg(Distinct, _t.Code).As("a")).Build();

        Assert.Equal("SELECT AVG(DISTINCT code) \"a\"", sql.Text);
    }

    [Fact]
    public void CountDistinct_Filter_CorrectSql()
    {
        SqlStatement sql = Select(Count(Distinct, _t.Code).Filter(_t.Code > 0).As("c")).Build();

        Assert.Equal("SELECT COUNT(DISTINCT code) FILTER (WHERE code > :0) \"c\"", sql.Text);
        Assert.Equal(0, sql.Parameters.Get<int>(":0"));
    }

    [Fact]
    public void CountDistinct_FilterAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Count(Distinct, _t.Code).Filter(ConditionIf(false, _t.Code > 0)))
            .From(_t)
            .Build());

        Assert.Equal(
            "An aggregate's FILTER requires a condition; omit it for an unfiltered aggregate.",
            ex.Message);
    }

    [Fact]
    public void DistinctAggregate_OffersOnlyTheEmptyAndPartitionOnlyWindows()
    {
        string[][] overs = [.. typeof(DistinctAggregateFunction)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Over")
            .Select(m => m.GetParameters().Select(p => p.ParameterType.Name).ToArray())
            .OrderBy(p => p.Length)];

        Assert.Equal([[], ["PartitionByClause"]], overs);
        Assert.False(typeof(AggregateFunction).IsAssignableFrom(typeof(DistinctAggregateFunction)));
    }

    [Fact]
    public void FilteredDistinctAggregate_OffersNoWindow()
    {
        Type filtered = typeof(FilteredDistinctAggregateFunction);

        Assert.DoesNotContain(
            filtered.GetMethods(BindingFlags.Public | BindingFlags.Instance),
            m => m.Name == "Over");
        Assert.False(typeof(AggregateFunction).IsAssignableFrom(filtered));
    }
}
