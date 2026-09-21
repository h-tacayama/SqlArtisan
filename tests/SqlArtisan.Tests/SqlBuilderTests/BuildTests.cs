
using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class BuildTests
{
    private readonly TestTable _t = new("t");

    // ParameterNameCache pre-formats the first 64 names per marker; the 65th is
    // the first formatted on demand, so the boundary is pinned on every marker.
    [Theory]
    [InlineData(Dbms.MySql, "?")]
    [InlineData(Dbms.Oracle, ":")]
    [InlineData(Dbms.PostgreSql, ":")]
    [InlineData(Dbms.Sqlite, ":")]
    [InlineData(Dbms.SqlServer, "@")]
    public void Build_PositionalMarkersPastTheCacheBoundary_StaySequential(Dbms dbms, string marker)
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code.In(Enumerable.Range(0, 70).ToArray()))
            .Build(dbms);

        Assert.Contains($"{marker}63, {marker}64, {marker}65", sql.Text);
        Assert.Equal(70, sql.Parameters.Count);
        Assert.Equal(63, sql.Parameters.Get<int>($"{marker}63"));
        Assert.Equal(64, sql.Parameters.Get<int>($"{marker}64"));
    }

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
