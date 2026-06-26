using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class PercentileTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void PercentileCont_WithinGroup_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(PercentileCont(0.5).WithinGroup(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileCont_WithinGroupOverEmpty_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY code) OVER ()";

        // Act
        SqlStatement sql =
            Select(PercentileCont(0.5).WithinGroup(OrderBy(_t.Code)).Over())
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileCont_WithinGroupOverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql =
            Select(
                PercentileCont(0.5)
                    .WithinGroup(OrderBy(_t.Code))
                    .Over(PartitionBy(_t.Name)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileCont_WithFractionAndAlias_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_CONT(0.9) WITHIN GROUP (ORDER BY code DESC) \"p90\"";

        // Act
        SqlStatement sql =
            Select(
                PercentileCont(0.9).WithinGroup(OrderBy(_t.Code.Desc)).As("p90"))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileDisc_WithinGroup_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY code)";

        // Act
        SqlStatement sql =
            Select(PercentileDisc(0.5).WithinGroup(OrderBy(_t.Code))).Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileDisc_WithinGroupOverPartitionBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY code) OVER (PARTITION BY name)";

        // Act
        SqlStatement sql =
            Select(
                PercentileDisc(0.5)
                    .WithinGroup(OrderBy(_t.Code))
                    .Over(PartitionBy(_t.Name)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void PercentileCont_WithNaNFraction_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PercentileCont(double.NaN));
    }

    [Fact]
    public void PercentileDisc_WithInfinityFraction_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PercentileDisc(double.PositiveInfinity));
    }
}
