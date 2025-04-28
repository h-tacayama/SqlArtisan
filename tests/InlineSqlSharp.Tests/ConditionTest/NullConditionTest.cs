namespace InlineSqlSharp.Tests;

public class NullConditionTest
{
    private readonly TestTable _t;
    private readonly ConditionTestAssert _assert;

    public NullConditionTest()
    {
        _t = new TestTable("t");
        _assert = new(_t);
    }

    [Fact]
    public void IsNull_Column_CorrectSql() =>
        _assert.Equal(_t.Name.IsNull, "\"t\".name IS NULL");

    [Fact]
    public void IsNotNull_Column_CorrectSql() =>
        _assert.Equal(_t.Name.IsNotNull, "\"t\".name IS NOT NULL");
}
