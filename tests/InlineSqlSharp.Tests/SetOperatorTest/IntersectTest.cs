using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class IntersectTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void INTERSECT_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            SELECT(1)
            .INTERSECT
            .SELECT(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("INTERSECT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void INTERSECT_SelectWithFrom_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .INTERSECT
            .SELECT(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("INTERSECT ");
        expected.Append("SELECT ");
        expected.Append(":0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void INTERSECT_SelectWithFromWhere_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .INTERSECT
            .SELECT(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0 ");
        expected.Append("INTERSECT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void INTERSECT_SelectWithFromWhereGroupBy_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .GROUP_BY(_t.code)
            .INTERSECT
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
        expected.Append("INTERSECT ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void INTERSECT_SelectWithFromWhereGroupByHaving_CorrectSql()
    {
        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == 1)
            .GROUP_BY(_t.code)
            .HAVING(COUNT(_t.code) > 0)
            .INTERSECT
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
        expected.Append("INTERSECT ");
        expected.Append("SELECT ");
        expected.Append(":2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void INTERSECT_ALL_SimpleSelect_CorrectSql()
    {
        SqlStatement sql =
            SELECT(1)
            .INTERSECT_ALL
            .SELECT(2)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0 ");
        expected.Append("INTERSECT ALL ");
        expected.Append("SELECT ");
        expected.Append(":1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
