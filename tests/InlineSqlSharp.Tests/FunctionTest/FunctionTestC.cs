using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void CONCAT_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            SELECT(CONCAT(_t.name, L("a"), L("b")))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CONCAT(\"t\".name, 'a', 'b')");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void COUNT_ColumnValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(COUNT(_t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void COUNT_DISTINCT_ColumnValue_CorrectSql()
    {
        SqlStatement sql =
            SELECT(COUNT(DISTINCT, _t.code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
