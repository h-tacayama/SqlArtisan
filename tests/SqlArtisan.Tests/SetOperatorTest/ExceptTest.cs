using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ExceptTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Except_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .Except
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("EXCEPT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Except_SelectWithFrom_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Except
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("EXCEPT ");
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Except_SelectWithFromWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .Except
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("EXCEPT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Except_SelectWithFromWhereGroupBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Except
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
        expected.Append("EXCEPT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Except_SelectWithFromWhereGroupByHaving_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Having(Count(_t.Code) > 0)
            .Except
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
        expected.Append("EXCEPT ");
        expected.Append("SELECT ");
        expected.Append(":2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ExceptAll_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .ExceptAll
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("EXCEPT ALL ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
