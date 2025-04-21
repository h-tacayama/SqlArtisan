namespace InlineSqlSharp.Tests;

public class ComparisonTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public ComparisonTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void Equal_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code == 2, "\"t\".code = :0", 1, 2);

    [Fact]
    public void NotEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code != 2, "\"t\".code <> :0", 1, 2);

    [Fact]
    public void LessThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code < 2, "\"t\".code < :0", 1, 2);

    [Fact]
    public void GreaterThan_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code > 2, "\"t\".code > :0", 1, 2);

    [Fact]
    public void LessEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code <= 2, "\"t\".code <= :0", 1, 2);

    [Fact]
    public void GreaterEqual_ColumnAndLiteral_CorrectSql() =>
        _assert.Equal(_t.code >= 2, "\"t\".code >= :0", 1, 2);
}
