namespace SqlArtisan.Tests;

public class BetweenTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public BetweenTest()
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
}
