
using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class BuildTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Build_MySql_QuestionMarkParameterPrefix()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("WHERE ");
        expected.Append("`t`.code = ?0");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Build_Oracle_ColonParameterPrefix()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Build_PostgreSql_ColonParameterPrefix()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Build_Sqlite_ColonParameterPrefix()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.Sqlite);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Build_SqlServer_AtSignParameterPrefix()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = @0");
        Assert.Equal(expected.ToString(), sql.Text);
    }
}
