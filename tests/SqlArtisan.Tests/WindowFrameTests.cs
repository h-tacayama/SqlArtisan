using System.Globalization;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class WindowFrameTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Rows_UnboundedPreceding_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT SUM(code) OVER (ORDER BY code ROWS UNBOUNDED PRECEDING)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(UnboundedPreceding)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Rows_Preceding_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT AVG(code) OVER (ORDER BY code ROWS 4 PRECEDING)";

        // Act
        SqlStatement sql =
            Select(Avg(_t.Code).Over(OrderBy(_t.Code).Rows(Preceding(4))))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RowsBetween_UnboundedPrecedingAndCurrentRow_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT SUM(code) OVER (ORDER BY code ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)";

        // Act
        SqlStatement sql =
            Select(
                Sum(_t.Code).Over(
                    OrderBy(_t.Code).RowsBetween(UnboundedPreceding, CurrentRow)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RangeBetween_PrecedingAndFollowing_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT SUM(code) OVER (ORDER BY code RANGE BETWEEN 10 PRECEDING AND 10 FOLLOWING)";

        // Act
        SqlStatement sql =
            Select(
                Sum(_t.Code).Over(
                    OrderBy(_t.Code).RangeBetween(Preceding(10), Following(10))))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RowsBetween_WithPartitionBy_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT SUM(code) OVER (PARTITION BY name ORDER BY code ROWS BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING)";

        // Act
        SqlStatement sql =
            Select(
                Sum(_t.Code).Over(
                    PartitionBy(_t.Name)
                    .OrderBy(_t.Code)
                    .RowsBetween(CurrentRow, UnboundedFollowing)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Preceding_NegativeOffsetUnderNonAsciiNegativeSignCulture_RendersInvariantSql()
    {
        // Arrange
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "\u2212";
        CultureInfo.CurrentCulture = culture;

        try
        {
            string expected =
                "SELECT SUM(code) OVER (ORDER BY code ROWS -1 PRECEDING)";

            // Act
            SqlStatement sql =
                Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(Preceding(-1))))
                .Build();

            // Assert
            Assert.Equal(expected, sql.Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
