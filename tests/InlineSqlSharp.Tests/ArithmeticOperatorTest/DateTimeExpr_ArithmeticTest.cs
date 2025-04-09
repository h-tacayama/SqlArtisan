using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DateTimeExpr_ArithmeticTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void DateOffset_AdditionWithLiteral_CorrectSql() =>
        Assert.Equal(
            "SELECT (\"t\".created_at + 1)",
            SELECT(_t.created_at + L(1)).Build().Text);

    [Fact]
    public void DateOffset_AdditionWithInt_CorrectSql() =>
        Assert.Equal(
            "SELECT (\"t\".created_at + :0)",
            SELECT(_t.created_at + 1).Build().Text);

    [Fact]
    public void DateOffset_SubtractionWithLiteral_CorrectSql() =>
        Assert.Equal(
            "SELECT (\"t\".created_at - 1)",
            SELECT(_t.created_at - L(1)).Build().Text);

    [Fact]
    public void DateOffset_SubtractionWithInt_CorrectSql() =>
        Assert.Equal(
            "SELECT (\"t\".created_at - :0)",
            SELECT(_t.created_at - 1).Build().Text);

    [Fact]
    public void DateDiff_SubtractionAndAddition_CorrectSql() =>
        Assert.Equal(
            "SELECT ((\"t\".created_at - \"t\".created_at) + 1)",
            SELECT((_t.created_at - _t.created_at) + L(1)).Build().Text);
}
