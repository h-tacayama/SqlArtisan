using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ArithmeticTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void Addition_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + 2).Build().Text);

    [Fact]
    public void Subtraction_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - 2).Build().Text);

    [Fact]
    public void Multiplication_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * 2).Build().Text);

    [Fact]
    public void Division_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / 2).Build().Text);

    [Fact]
    public void Modulus_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % 2).Build().Text);

    [Fact]
    public void SubtractionAndAddition_nesting_CorrectSql() =>
        Assert.Equal(
            "SELECT ((\"t\".created_at - \"t\".created_at) + :0)",
            SELECT((_t.created_at - _t.created_at) + 1).Build().Text);
}
