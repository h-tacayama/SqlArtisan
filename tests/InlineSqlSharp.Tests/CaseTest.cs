using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CaseTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void CASE_SearchCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    [
                        WHEN(_t.name == "a").THEN("A"),
                        WHEN(_t.name == 'b').THEN('B'),
                        WHEN(_t.name == "c").THEN("C"),
                    ],
                    ELSE("Z")))
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
        Assert.Equal(7, sql.ParameterCount);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal('b', sql.Parameters.Get<char>(":2"));
        Assert.Equal('B', sql.Parameters.Get<char>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":6"));
    }

    [Fact]
    public void CASE_SimpleCaseWithCharacterExpr_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CASE(
                    _t.name,
                    [
                        WHEN("a").THEN("A"),
                        WHEN('b').THEN('B'),
                        WHEN("c").THEN("C"),
                    ],
                    ELSE("Z")))
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
        Assert.Equal(7, sql.ParameterCount);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("A", sql.Parameters.Get<string>(":1"));
        Assert.Equal('b', sql.Parameters.Get<char>(":2"));
        Assert.Equal('B', sql.Parameters.Get<char>(":3"));
        Assert.Equal("c", sql.Parameters.Get<string>(":4"));
        Assert.Equal("C", sql.Parameters.Get<string>(":5"));
        Assert.Equal("Z", sql.Parameters.Get<string>(":6"));
    }
}
