namespace InlineSqlSharp.Tests;

public class BetweenTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public BetweenTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void BETWEEN_Literals_CorrectSql() =>
        _assert.Equal(_t.code.BETWEEN(1, 10),
            "\"t\".code BETWEEN :0 AND :1",
            2, 1, 10);

    [Fact]
    public void NOT_BETWEEN_Literals_CorrectSql() =>
        _assert.Equal(_t.code.NOT_BETWEEN(1, 10),
            "\"t\".code NOT BETWEEN :0 AND :1",
            2, 1, 10);
}
