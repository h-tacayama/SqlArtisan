using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class LeftJoinLateralTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly DerivedTable _x = new("x");

    [Fact]
    public void LeftJoinLateral_PostgreSql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .LeftJoinLateral(
                Select(_s.Code).From(_s).Where(_s.Code == _t.Code),
                _x)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("LEFT JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("x ");
        expected.Append("ON TRUE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LeftJoinLateral_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .LeftJoinLateral(
                Select(_s.Code).From(_s).Where(_s.Code == _t.Code),
                _x)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("LEFT JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT `s`.code FROM test_table `s` WHERE `s`.code = `t`.code");
        expected.Append(") ");
        expected.Append("x ");
        expected.Append("ON TRUE");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
