using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class OuterApplyTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly DerivedTable _x = new("x");

    [Fact]
    public void OuterApply_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .OuterApply(
                Select(_s.Code).From(_s).Where(_s.Code == _t.Code),
                _x)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("OUTER APPLY ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("x");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OuterApply_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .OuterApply(
                Select(_s.Code).From(_s).Where(_s.Code == _t.Code),
                _x)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("OUTER APPLY ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("x");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
