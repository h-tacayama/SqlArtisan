using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class UnionTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Union_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .Union
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("UNION ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Union_SelectWithFrom_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Union
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("UNION ");
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Union_SelectWithFromWhere_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .Union
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("UNION ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Union_SelectWithFromWhereGroupBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Union
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
        expected.Append("UNION ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Union_SelectWithFromWhereGroupByHaving_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1)
            .GroupBy(_t.Code)
            .Having(Count(_t.Code) > 0)
            .Union
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
        expected.Append("UNION ");
        expected.Append("SELECT ");
        expected.Append(":2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void UnionAll_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            Select(1)
            .UnionAll
            .Select(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("UNION ALL ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
