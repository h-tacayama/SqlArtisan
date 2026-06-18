using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class HavingTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Having_SingleCondition_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Having(Count(_t.Name) > 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name ");
        expected.Append("HAVING ");
        expected.Append("COUNT(\"t\".name) > :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Having_FollowedByOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Having(Count(_t.Name) > 1)
            .OrderBy(_t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name ");
        expected.Append("HAVING ");
        expected.Append("COUNT(\"t\".name) > :0 ");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Having_FollowedByOrderByAndLimit_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Having(Count(_t.Name) > 1)
            .OrderBy(Count(_t.Name).Desc)
            .Limit(10)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name ");
        expected.Append("HAVING ");
        expected.Append("COUNT(\"t\".name) > :0 ");
        expected.Append("ORDER BY ");
        expected.Append("COUNT(\"t\".name) DESC ");
        expected.Append("LIMIT :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
