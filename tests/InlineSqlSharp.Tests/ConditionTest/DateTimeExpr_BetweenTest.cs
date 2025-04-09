namespace InlineSqlSharp.Tests;

public class DateTimeExpr_BetweenTest
{
    private readonly test_table _t;
    private readonly ConditionTestAssert _assert;

    public DateTimeExpr_BetweenTest()
    {
        _t = new test_table("t");
        _assert = new(_t);
    }

    [Fact]
    public void DateTimeExpr_BETWEEN_Columns_CorrectSql() =>
        _assert.Equal(
            _t.created_at.BETWEEN(_t.created_at, _t.created_at),
            "\"t\".created_at BETWEEN \"t\".created_at AND \"t\".created_at");

    [Fact]
    public void DateTimeExpr_BETWEEN_ColumnAndValue_CorrectSql() =>
        _assert.Equal(
            _t.created_at.BETWEEN(_t.created_at, new DateTime(2001, 2, 3)),
            "\"t\".created_at BETWEEN \"t\".created_at AND :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_BETWEEN_ValueAndColumn_CorrectSql() =>
        _assert.Equal(
            _t.created_at.BETWEEN(new DateTime(2001, 2, 3), _t.created_at),
            "\"t\".created_at BETWEEN :0 AND \"t\".created_at",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_BETWEEN_Values_CorrectSql() =>
        _assert.Equal(
            _t.created_at.BETWEEN(new DateTime(2001, 2, 3), new DateTime(2004, 5, 6)),
            "\"t\".created_at BETWEEN :0 AND :1",
            2, new DateTime(2001, 2, 3), new DateTime(2004, 5, 6));

    [Fact]
    public void DateTimeExpr_NOT_BETWEEN_Columns_CorrectSql() =>
        _assert.Equal(
            _t.created_at.NOT_BETWEEN(_t.created_at, _t.created_at),
            "\"t\".created_at NOT BETWEEN \"t\".created_at AND \"t\".created_at");

    [Fact]
    public void DateTimeExpr_NOT_BETWEEN_ColumnAndValue_CorrectSql() =>
        _assert.Equal(
            _t.created_at.NOT_BETWEEN(_t.created_at, new DateTime(2001, 2, 3)),
            "\"t\".created_at NOT BETWEEN \"t\".created_at AND :0",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_NOT_BETWEEN_ValueAndColumn_CorrectSql() =>
        _assert.Equal(
            _t.created_at.NOT_BETWEEN(new DateTime(2001, 2, 3), _t.created_at),
            "\"t\".created_at NOT BETWEEN :0 AND \"t\".created_at",
            1, new DateTime(2001, 2, 3));

    [Fact]
    public void DateTimeExpr_NOT_BETWEEN_Values_CorrectSql() =>
        _assert.Equal(
            _t.created_at.NOT_BETWEEN(new DateTime(2001, 2, 3), new DateTime(2004, 5, 6)),
            "\"t\".created_at NOT BETWEEN :0 AND :1",
            2, new DateTime(2001, 2, 3), new DateTime(2004, 5, 6));
}
