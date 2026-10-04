namespace SqlArtisan.Tests;

public class BetweenTests
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public BetweenTests()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void Between_Literals_CorrectSql() =>
        _assert.Equal(_t.Code.Between(1, 10),
            "\"t\".code BETWEEN :0 AND :1",
            2, 1, 10);

    [Fact]
    public void NotBetween_Literals_CorrectSql() =>
        _assert.Equal(_t.Code.NotBetween(1, 10),
            "\"t\".code NOT BETWEEN :0 AND :1",
            2, 1, 10);

    [Fact]
    public void Between_NullLowerBound_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => _t.Code.Between(null!, 10));

        Assert.Equal(
            "BETWEEN cannot compare a C# null; test for NULL with .IsNull or .IsNotNull. "
                + "(Parameter 'rightSide1')",
            ex.Message);
    }

    [Fact]
    public void NotBetween_NullUpperBound_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => _t.Code.NotBetween(1, null!));

        Assert.Equal(
            "NOT BETWEEN cannot compare a C# null; test for NULL with .IsNull or .IsNotNull. "
                + "(Parameter 'rightSide2')",
            ex.Message);
    }
}
