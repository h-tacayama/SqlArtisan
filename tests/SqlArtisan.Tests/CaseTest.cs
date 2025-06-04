using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class CaseTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Case_SearchCaseWithoutElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == 'b').Then('B'),
                    When(_t.Name == 'c').Then('C')))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith1WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("ELSE :2 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith2WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("ELSE :4 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith3WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("ELSE :6 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith4WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("ELSE :8 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith5WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("ELSE :10 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith6WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    When(_t.Name == "f").Then("F"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("ELSE :12 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith7WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    When(_t.Name == "f").Then("F"),
                    When(_t.Name == "g").Then("G"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("WHEN (\"t\".name = :12) THEN :13 ")
            .Append("ELSE :14 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith8WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    When(_t.Name == "f").Then("F"),
                    When(_t.Name == "g").Then("G"),
                    When(_t.Name == "h").Then("H"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("WHEN (\"t\".name = :12) THEN :13 ")
            .Append("WHEN (\"t\".name = :14) THEN :15 ")
            .Append("ELSE :16 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith9WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    When(_t.Name == "f").Then("F"),
                    When(_t.Name == "g").Then("G"),
                    When(_t.Name == "h").Then("H"),
                    When(_t.Name == "i").Then("I"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("WHEN (\"t\".name = :12) THEN :13 ")
            .Append("WHEN (\"t\".name = :14) THEN :15 ")
            .Append("WHEN (\"t\".name = :16) THEN :17 ")
            .Append("ELSE :18 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith10WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    When(_t.Name == "a").Then("A"),
                    When(_t.Name == "b").Then("B"),
                    When(_t.Name == "c").Then("C"),
                    When(_t.Name == "d").Then("D"),
                    When(_t.Name == "e").Then("E"),
                    When(_t.Name == "f").Then("F"),
                    When(_t.Name == "g").Then("G"),
                    When(_t.Name == "h").Then("H"),
                    When(_t.Name == "i").Then("I"),
                    When(_t.Name == "j").Then("J"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("WHEN (\"t\".name = :12) THEN :13 ")
            .Append("WHEN (\"t\".name = :14) THEN :15 ")
            .Append("WHEN (\"t\".name = :16) THEN :17 ")
            .Append("WHEN (\"t\".name = :18) THEN :19 ")
            .Append("ELSE :20 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Case_SearchCaseWith11WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    [
                        When(_t.Name == "a").Then("A"),
                        When(_t.Name == "b").Then("B"),
                        When(_t.Name == "c").Then("C"),
                        When(_t.Name == "d").Then("D"),
                        When(_t.Name == "e").Then("E"),
                        When(_t.Name == "f").Then("F"),
                        When(_t.Name == "g").Then("G"),
                        When(_t.Name == "h").Then("H"),
                        When(_t.Name == "i").Then("I"),
                        When(_t.Name == "j").Then("J"),
                        When(_t.Name == "k").Then("K"),
                    ],
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("WHEN (\"t\".name = :0) THEN :1 ")
            .Append("WHEN (\"t\".name = :2) THEN :3 ")
            .Append("WHEN (\"t\".name = :4) THEN :5 ")
            .Append("WHEN (\"t\".name = :6) THEN :7 ")
            .Append("WHEN (\"t\".name = :8) THEN :9 ")
            .Append("WHEN (\"t\".name = :10) THEN :11 ")
            .Append("WHEN (\"t\".name = :12) THEN :13 ")
            .Append("WHEN (\"t\".name = :14) THEN :15 ")
            .Append("WHEN (\"t\".name = :16) THEN :17 ")
            .Append("WHEN (\"t\".name = :18) THEN :19 ")
            .Append("WHEN (\"t\".name = :20) THEN :21 ")
            .Append("ELSE :22 ")
            .Append("END");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(23, sql.Parameters.Count);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal("b", sql.Parameters.Get<string>(":2"));
        Assert.Equal("B", sql.Parameters.Get<string>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("d", sql.Parameters.Get<string>(":6"));
        Assert.Equal("D", sql.Parameters.Get<string>(":7"));
        Assert.Equal("e", sql.Parameters.Get<string>(":8"));
        Assert.Equal("E", sql.Parameters.Get<string>(":9"));
        Assert.Equal("f", sql.Parameters.Get<string>(":10"));
        Assert.Equal("F", sql.Parameters.Get<string>(":11"));
        Assert.Equal("g", sql.Parameters.Get<string>(":12"));
        Assert.Equal("G", sql.Parameters.Get<string>(":13"));
        Assert.Equal("h", sql.Parameters.Get<string>(":14"));
        Assert.Equal("H", sql.Parameters.Get<string>(":15"));
        Assert.Equal("i", sql.Parameters.Get<string>(":16"));
        Assert.Equal("I", sql.Parameters.Get<string>(":17"));
        Assert.Equal("j", sql.Parameters.Get<string>(":18"));
        Assert.Equal("J", sql.Parameters.Get<string>(":19"));
        Assert.Equal("k", sql.Parameters.Get<string>(":20"));
        Assert.Equal("K", sql.Parameters.Get<string>(":21"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":22"));
    }

    [Fact]
    public void Case_SimpleCaseWithoutElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When('b').Then('B'),
                    When("c").Then("C")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith1WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("ELSE :2 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith2WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("ELSE :4 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith3WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("ELSE :6 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith4WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("ELSE :8 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith5WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("ELSE :10 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith6WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    When("f").Then("F"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("ELSE :12 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith7WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    When("f").Then("F"),
                    When("g").Then("G"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("WHEN :12 THEN :13 ")
            .Append("ELSE :14 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith8WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    When("f").Then("F"),
                    When("g").Then("G"),
                    When("h").Then("H"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("WHEN :12 THEN :13 ")
            .Append("WHEN :14 THEN :15 ")
            .Append("ELSE :16 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith9WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    When("f").Then("F"),
                    When("g").Then("G"),
                    When("h").Then("H"),
                    When("i").Then("I"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("WHEN :12 THEN :13 ")
            .Append("WHEN :14 THEN :15 ")
            .Append("WHEN :16 THEN :17 ")
            .Append("ELSE :18 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith10WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    When("a").Then("A"),
                    When("b").Then("B"),
                    When("c").Then("C"),
                    When("d").Then("D"),
                    When("e").Then("E"),
                    When("f").Then("F"),
                    When("g").Then("G"),
                    When("h").Then("H"),
                    When("i").Then("I"),
                    When("j").Then("J"),
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("WHEN :12 THEN :13 ")
            .Append("WHEN :14 THEN :15 ")
            .Append("WHEN :16 THEN :17 ")
            .Append("WHEN :18 THEN :19 ")
            .Append("ELSE :20 ")
            .Append("END");
    }

    [Fact]
    public void Case_SimpleCaseWith11WhenAndElse_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Case(
                    _t.Name,
                    [
                        When("a").Then("A"),
                        When("b").Then("B"),
                        When("c").Then("C"),
                        When("d").Then("D"),
                        When("e").Then("E"),
                        When("f").Then("F"),
                        When("g").Then("G"),
                        When("h").Then("H"),
                        When("i").Then("I"),
                        When("j").Then("J"),
                        When("k").Then("K"),
                    ],
                    Else("Z")))
            .Build();

        var expected = new StringBuilder()
            .Append("SELECT ")
            .Append("CASE ")
            .Append("\"t\".name ")
            .Append("WHEN :0 THEN :1 ")
            .Append("WHEN :2 THEN :3 ")
            .Append("WHEN :4 THEN :5 ")
            .Append("WHEN :6 THEN :7 ")
            .Append("WHEN :8 THEN :9 ")
            .Append("WHEN :10 THEN :11 ")
            .Append("WHEN :12 THEN :13 ")
            .Append("WHEN :14 THEN :15 ")
            .Append("WHEN :16 THEN :17 ")
            .Append("WHEN :18 THEN :19 ")
            .Append("WHEN :20 THEN :21 ")
            .Append("ELSE :22 ")
            .Append("END");

        Assert.Equal(23, sql.Parameters.Count);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal("b", sql.Parameters.Get<string>(":2"));
        Assert.Equal("B", sql.Parameters.Get<string>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("d", sql.Parameters.Get<string>(":6"));
        Assert.Equal("D", sql.Parameters.Get<string>(":7"));
        Assert.Equal("e", sql.Parameters.Get<string>(":8"));
        Assert.Equal("E", sql.Parameters.Get<string>(":9"));
        Assert.Equal("f", sql.Parameters.Get<string>(":10"));
        Assert.Equal("F", sql.Parameters.Get<string>(":11"));
        Assert.Equal("g", sql.Parameters.Get<string>(":12"));
        Assert.Equal("G", sql.Parameters.Get<string>(":13"));
        Assert.Equal("h", sql.Parameters.Get<string>(":14"));
        Assert.Equal("H", sql.Parameters.Get<string>(":15"));
        Assert.Equal("i", sql.Parameters.Get<string>(":16"));
        Assert.Equal("I", sql.Parameters.Get<string>(":17"));
        Assert.Equal("j", sql.Parameters.Get<string>(":18"));
        Assert.Equal("J", sql.Parameters.Get<string>(":19"));
        Assert.Equal("k", sql.Parameters.Get<string>(":20"));
        Assert.Equal("K", sql.Parameters.Get<string>(":21"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":22"));
    }
}
