using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Var_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Var(_t.Code))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("VAR(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void VarPop_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(VarPop(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("VAR_POP(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void VarSamp_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(VarSamp(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("VAR_SAMP(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Variance_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Variance(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("VARIANCE(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Varp_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Varp(_t.Code))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("VARP(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
