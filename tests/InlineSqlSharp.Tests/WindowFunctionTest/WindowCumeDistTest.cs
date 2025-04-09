using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class WindowCumeDistTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void CUME_DIST_OVER_PartitionByOrderBy_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CUME_DIST().OVER(
                    PARTITION_BY(_t.code, _t.name)
                    .ORDER_BY(_t.code.ASC, _t.name.DESC)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CUME_DIST() ");
        expected.Append("OVER ");
        expected.Append("(");
        expected.Append("PARTITION BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".code ASC, ");
        expected.Append("\"t\".name DESC");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void CUME_DIST_OVER_OrderBy_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                CUME_DIST().OVER(
                    ORDER_BY(_t.code, _t.name)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CUME_DIST() ");
        expected.Append("OVER ");
        expected.Append("(");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
