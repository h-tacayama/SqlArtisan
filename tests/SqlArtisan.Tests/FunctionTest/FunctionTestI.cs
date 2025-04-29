using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Instr_BasicPattern_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Instr_WithPosition_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc", 1))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Instr_WithOccurrence_CorrectSql()
    {
        SqlStatement sql =
            Select(Instr(_t.Name, "abc", 1, 2))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("INSTR(\"t\".name, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
