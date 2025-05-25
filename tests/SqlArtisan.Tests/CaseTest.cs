using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class CaseTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Case_SearchCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    [
                        When(_t.Name == "a").Then("A"),
                        When(_t.Name == 'b').Then('B'),
                        When(_t.Name == "c").Then("C"),
                    ],
                    Else("Z")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("WHEN (\"t\".name = :0) THEN :1 ");
        expected.Append("WHEN (\"t\".name = :2) THEN :3 ");
        expected.Append("WHEN (\"t\".name = :4) THEN :5 ");
        expected.Append("ELSE :6 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(7, sql.Parameters.Count);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal('b', sql.Parameters.Get<char>(":2"));
        Assert.Equal('B', sql.Parameters.Get<char>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":6"));
    }

    [Fact]
    public void Case_SimpleCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    [
                        When("a").Then("A"),
                        When('b').Then('B'),
                        When("c").Then("C"),
                    ],
                    Else("Z")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CASE ");
        expected.Append("\"t\".name ");
        expected.Append("WHEN :0 THEN :1 ");
        expected.Append("WHEN :2 THEN :3 ");
        expected.Append("WHEN :4 THEN :5 ");
        expected.Append("ELSE :6 ");
        expected.Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(7, sql.Parameters.Count);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal('b', sql.Parameters.Get<char>(":2"));
        Assert.Equal('B', sql.Parameters.Get<char>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":6"));
    }
}
