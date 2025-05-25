using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Coalesce_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Coalesce(_t.Code, _t.Name, "other"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COALESCE(\"t\".code, \"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

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

    [Fact]
    public void CurrentDate_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentDate).Build();
        Assert.Equal("SELECT CURRENT_DATE", sql.Text);
    }

    [Fact]
    public void CurrentTime_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentTime).Build();
        Assert.Equal("SELECT CURRENT_TIME", sql.Text);
    }

    [Fact]
    public void CurrentTimestamp_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentTimestamp).Build();
        Assert.Equal("SELECT CURRENT_TIMESTAMP", sql.Text);
    }

    [Fact]
    public void CurrVal_SequenceName_CorrectSql()
    {
        SqlStatement sql =
            Select(CurrVal("seq_test"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CURRVAL('seq_test')");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
