using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Tests;

public class ArithmeticTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Addition_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", Select(_t.Code + 2).Build().Text);

    [Fact]
    public void Subtraction_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", Select(_t.Code - 2).Build().Text);

    [Fact]
    public void Multiplication_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", Select(_t.Code * 2).Build().Text);

    [Fact]
    public void Division_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", Select(_t.Code / 2).Build().Text);

    [Fact]
    public void Modulus_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", Select(_t.Code % 2).Build().Text);

    [Fact]
    public void SubtractionAndAddition_nesting_CorrectSql() =>
        Assert.Equal(
            "SELECT ((\"t\".created_at - \"t\".created_at) + :0)",
            Select((_t.CreatedAt - _t.CreatedAt) + 1).Build().Text);
}
