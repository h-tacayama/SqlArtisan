using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Greatest_NumericValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Greatest(_t.Code, 10, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GREATEST(\"t\".code, :0, \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Greatest_NoExpressions_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => Greatest());

        Assert.Equal("GREATEST requires at least one expression.", ex.Message);
    }

    [Fact]
    public void Grouping_SingleColumn_CorrectSql()
    {
        SqlStatement sql =
            Select(Grouping(_t.Code))
            .From(_t)
            .GroupBy(Rollup(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GROUPING(\"t\".code) ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Grouping_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(Grouping(_t.Code, _t.Name))
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GROUPING(\"t\".code, \"t\".name) ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // Past the two declared parameters the arguments arrive as a params tail
    // merged after them, so this pins the merged order.
    [Fact]
    public void Grouping_MoreColumnsThanDeclaredParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(Grouping(_t.Code, _t.Name, _t.CreatedAt))
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name, _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GROUPING(\"t\".code, \"t\".name, \"t\".created_at) ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code, \"t\".name, \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingId_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(GroupingId(_t.Code, _t.Name))
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("GROUPING_ID(\"t\".code, \"t\".name) ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
