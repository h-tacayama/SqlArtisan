using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class MinusTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Minus_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .Minus
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("MINUS ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Minus_SelectWithFrom_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Minus
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("MINUS ");
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Minus_SelectWithFromWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .Minus
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("MINUS ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Minus_SelectWithFromWhereGroupBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Minus
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".code ");
        expected.Append("MINUS ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Minus_SelectWithFromWhereGroupByHaving_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Having(Count(_t.Code) > 0)
            .Minus
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".code ");
        expected.Append("HAVING ");
        expected.Append("COUNT(\"t\".code) > :1 ");
        expected.Append("MINUS ");
        expected.Append("SELECT ");
        expected.Append(":2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MinusAll_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .MinusAll
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("MINUS ALL ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
