namespace InlineSqlSharp.Tests;

public class NullConditionTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public NullConditionTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void IS_NULL_Column_CorrectSql() =>
        _assert.Equal(_t.name.IS_NULL, "\"t\".name IS NULL");

    [Fact]
    public void IS_NOT_NULL_Column_CorrectSql() =>
        _assert.Equal(_t.name.IS_NOT_NULL, "\"t\".name IS NOT NULL");
}
