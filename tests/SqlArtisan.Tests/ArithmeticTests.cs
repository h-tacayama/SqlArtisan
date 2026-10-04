using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ArithmeticTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Addition_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", Select(_t.Code + 2).Build().Text);

    [Fact]
    public void Addition_StringAndColumn_BindsTheString()
    {
        SqlStatement sql = Select("Dr. " + _t.Name).Build();

        Assert.Equal("SELECT (:0 + \"t\".name)", sql.Text);
        Assert.Equal("Dr. ", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Addition_StringColumnString_ChainsLeftToRight()
    {
        SqlStatement sql = Select("(" + _t.Name + ")").Build();

        Assert.Equal("SELECT ((:0 + \"t\".name) + :1)", sql.Text);
        Assert.Equal("(", sql.Parameters.Get<string>(":0"));
        Assert.Equal(")", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Addition_ColumnAndNestedStringLeftSum_NestsTheSum()
    {
        SqlStatement sql = Select(_t.Name + (" " + _t.Code)).Build();

        Assert.Equal("SELECT (\"t\".name + (:0 + \"t\".code))", sql.Text);
        Assert.Equal(" ", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Addition_StringAndColumnInWhere_ComparesTheConcatenation()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Name == "Dr. " + _t.Name)
            .Build(Dbms.SqlServer);

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".name = (@0 + \"t\".name)",
            sql.Text);
        Assert.Equal("Dr. ", sql.Parameters.Get<string>("@0"));
    }

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
    public void SubtractionAndAddition_Nesting_CorrectSql() =>
        Assert.Equal(
            "SELECT ((\"t\".created_at - \"t\".created_at) + :0)",
            Select((_t.CreatedAt - _t.CreatedAt) + 1).Build().Text);
}
