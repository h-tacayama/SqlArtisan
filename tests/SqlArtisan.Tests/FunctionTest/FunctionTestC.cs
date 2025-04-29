using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Concat_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Concat(_t.Name, "a", "b"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CONCAT(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Count_ColumnValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Count(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Count_Distinct_ColumnValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Count(Distinct, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
