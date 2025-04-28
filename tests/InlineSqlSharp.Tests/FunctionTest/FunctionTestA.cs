using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Abs_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Abs(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ABS(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void AddMonths_DateTimeAndNumeric_CorrectSql()
    {
        SqlStatement sql =
            Select(AddMonths(_t.CreatedAt, 3))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("ADD_MONTHS(\"t\".created_at, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Avg_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Avg(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("AVG(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Avg_Distinct_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Avg(Distinct, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("AVG(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
