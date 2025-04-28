using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class WindowRowNumberTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void RowNumber_Over_PartitionByOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(
                RowNumber().Over(
                    PartitionBy(_t.Code, _t.Name)
                    .OrderBy(_t.Code.Asc, _t.Name.Desc)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ROW_NUMBER() ");
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
    public void RowNumber_Over_OrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(RowNumber().Over(OrderBy(_t.Code, _t.Name)))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ROW_NUMBER() ");
        expected.Append("OVER ");
        expected.Append("(");
        expected.Append("ORDER BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name");
        expected.Append(")");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
