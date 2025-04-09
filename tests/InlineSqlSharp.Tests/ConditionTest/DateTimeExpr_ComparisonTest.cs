using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DateTimeExpr_ComparisonTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public DateTimeExpr_ComparisonTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void DateTimeExpr_Equal_ColumnAndParameter_CorrectSql() =>
        _assert.Equal(_t.created_at == P(new DateTime(2001, 2, 3)),
            "\"t\".created_at = :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_Equal_ColumnAndValue_CorrectSql() =>
        _assert.Equal(_t.created_at == new DateTime(2001, 2, 3),
            "\"t\".created_at = :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_NotEqual_ColumnAndParameter_CorrectSql() =>
        _assert.Equal(_t.created_at != P(new DateTime(2001, 2, 3)),
            "\"t\".created_at <> :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_NotEqual_ColumnAndValue_CorrectSql() =>
        _assert.Equal(_t.created_at != new DateTime(2001, 2, 3),
            "\"t\".created_at <> :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_LessThan_ColumnAndParameter_CorrectSql() =>
        _assert.Equal(_t.created_at < P(new DateTime(2001, 2, 3)),
            "\"t\".created_at < :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_LessThan_ColumnAndValue_CorrectSql() =>
        _assert.Equal(_t.created_at < new DateTime(2001, 2, 3),
            "\"t\".created_at < :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_GreaterThan_ColumnAndParameter_CorrectSql() =>
        _assert.Equal(_t.created_at > P(new DateTime(2001, 2, 3)),
            "\"t\".created_at > :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_GreaterThan_ColumnAndValue_CorrectSql() =>
        _assert.Equal(_t.created_at > new DateTime(2001, 2, 3),
            "\"t\".created_at > :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_GreaterEqual_ColumnAndParameter_CorrectSql() =>
        _assert.Equal(_t.created_at >= P(new DateTime(2001, 2, 3)),
            "\"t\".created_at >= :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_GreaterEqual_ColumnAndValue_CorrectSql() =>
        _assert.Equal(_t.created_at >= new DateTime(2001, 2, 3),
            "\"t\".created_at >= :0",
            1, new DateTime(2001, 2, 3));
}
