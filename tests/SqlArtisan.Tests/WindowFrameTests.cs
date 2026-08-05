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
    public void Preceding_NegativeOffset_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Preceding(-1));

        Assert.Equal("PRECEDING requires a non-negative offset.", ex.Message);
    }

    [Fact]
    public void Preceding_ZeroOffset_CorrectSql()
    {
        // Arrange \u2014 0 PRECEDING is CURRENT ROW's own spelling; the guard only
        // rejects a negative offset, so zero must still build.
        string expected = "SELECT SUM(code) OVER (ORDER BY code ROWS 0 PRECEDING)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(Preceding(0))))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void Following_NegativeOffset_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Following(-1));

        Assert.Equal("FOLLOWING requires a non-negative offset.", ex.Message);
    }

    [Fact]
    public void Rows_SoleFollowing_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(Following(1)))).Build());

        Assert.Equal(
            "A window frame with a single bound must not be later than CURRENT ROW.", ex.Message);
    }

    [Fact]
    public void Rows_SoleUnboundedFollowing_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(UnboundedFollowing))).Build());

        Assert.Equal(
            "A window frame with a single bound must not be later than CURRENT ROW.", ex.Message);
    }

    [Fact]
    public void Rows_SoleCurrentRow_CorrectSql()
    {
        // Arrange
        string expected = "SELECT SUM(code) OVER (ORDER BY code ROWS CURRENT ROW)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).Rows(CurrentRow)))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RowsBetween_CurrentRowThenPreceding_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).RowsBetween(CurrentRow, Preceding(1)))).Build());

        Assert.Equal(
            "A window frame's BETWEEN start bound must not be later than its end bound.", ex.Message);
    }

    [Fact]
    public void RowsBetween_UnboundedFollowingThenCurrentRow_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).RowsBetween(UnboundedFollowing, CurrentRow))).Build());

        Assert.Equal(
            "A window frame's BETWEEN start bound must not be later than its end bound.", ex.Message);
    }

    [Fact]
    public void RowsBetween_CurrentRowThenUnboundedPreceding_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).RowsBetween(CurrentRow, UnboundedPreceding))).Build());

        Assert.Equal(
            "A window frame's BETWEEN start bound must not be later than its end bound.", ex.Message);
    }

    [Fact]
    public void RowsBetween_SameKindDescendingPreceding_CorrectSql()
    {
        // Arrange \u2014 3 PRECEDING then 5 PRECEDING is a legal (if empty) frame; the
        // guard compares bound kind, never the offset, so this must still build.
        string expected =
            "SELECT SUM(code) OVER (ORDER BY code ROWS BETWEEN 3 PRECEDING AND 5 PRECEDING)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).RowsBetween(Preceding(3), Preceding(5))))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RowsBetween_SameKindDescendingFollowing_CorrectSql()
    {
        // Arrange
        string expected =
            "SELECT SUM(code) OVER (ORDER BY code ROWS BETWEEN 5 FOLLOWING AND 3 FOLLOWING)";

        // Act
        SqlStatement sql =
            Select(Sum(_t.Code).Over(OrderBy(_t.Code).RowsBetween(Following(5), Following(3))))
            .Build();

        // Assert
        Assert.Equal(expected, sql.Text);
    }

    [Fact]
    public void RowsBetween_UnboundedPrecedingThenUnboundedPreceding_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(
                OrderBy(_t.Code).RowsBetween(UnboundedPreceding, UnboundedPreceding)))
            .Build());

        Assert.Equal(
            "A window frame's BETWEEN end bound must not be UNBOUNDED PRECEDING.", ex.Message);
    }

    [Fact]
    public void RowsBetween_UnboundedFollowingThenUnboundedFollowing_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(Sum(_t.Code).Over(
                OrderBy(_t.Code).RowsBetween(UnboundedFollowing, UnboundedFollowing)))
            .Build());

        Assert.Equal(
            "A window frame's BETWEEN start bound must not be UNBOUNDED FOLLOWING.", ex.Message);
    }
}
