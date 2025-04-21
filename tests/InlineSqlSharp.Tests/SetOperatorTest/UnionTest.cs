using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class UnionTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void UNION_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            SELECT(1)
            .UNION
            .SELECT(2)
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
    public void UNION_SelectWithFrom_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .UNION
            .SELECT(2)
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
    public void UNION_SelectWithFromWhere_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .UNION
            .SELECT(2)
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
    public void UNION_SelectWithFromWhereGroupBy_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .GROUP_BY(_t.code)
            .UNION
            .SELECT(2)
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
    public void UNION_SelectWithFromWhereGroupByHaving_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .GROUP_BY(_t.code)
            .HAVING(COUNT(_t.code) > 0)
            .UNION
            .SELECT(2)
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
    public void UNION_ALL_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            SELECT(1)
            .UNION_ALL
            .SELECT(2)
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
