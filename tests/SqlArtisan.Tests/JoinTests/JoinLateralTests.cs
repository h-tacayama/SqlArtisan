using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class JoinLateralTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly DerivedTable _x = new("x");

    [Fact]
    public void JoinLateral_PostgreSql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .JoinLateral(Select(_s.Code).From(_s), _x)
            .On(_t.Code == _x.Column("code"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code FROM test_table \"s\"");
        expected.Append(") ");
        expected.Append("x ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"x\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void JoinLateral_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .JoinLateral(Select(_s.Code).From(_s), _x)
            .On(_t.Code == _x.Column("code"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT `s`.code FROM test_table `s`");
        expected.Append(") ");
        expected.Append("x ");
        expected.Append("ON ");
        expected.Append("`t`.code = `x`.code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void JoinLateral_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .JoinLateral(Select(_s.Code).From(_s), _x)
            .On(_t.Code == _x.Column("code"))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code FROM test_table \"s\"");
        expected.Append(") ");
        expected.Append("x ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"x\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
