using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

[Collection("NonParallelTests")]
public class ConfigTests : IDisposable
{
    private readonly TestTable _t = new("t");

    public void Dispose()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.PostgreSql);
    }

    [Fact]
    public void SetDefaultDbms_SelectForMySql_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.MySql);

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1)
            .Build();

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
    public void SetDefaultDbms_UpdateForOracle_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.Oracle);

        SqlStatement sql =
            Update(_t)
            .Set(_t.Name == "New Name")
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        // Oracle rejects AS on a table alias (ORA-00933): alias follows with a
        // space. The SET target stays unqualified; the WHERE qualifies via alias.
        expected.Append("UPDATE test_table \"t\" ");
        expected.Append("SET name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SetDefaultDbms_DeleteForSqlServer_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.SqlServer);

        // Unaliased: this test verifies the SQL Server parameter marker (@). The
        // dialect-correct aliased DELETE form (DELETE x FROM t AS x) is a separate
        // follow-up (issue #96 SQL Server scope), so an alias is omitted here to
        // keep the asserted statement executable on SQL Server.
        TestTable t = new();

        SqlStatement sql =
            DeleteFrom(t)
            .Where(t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table ");
        expected.Append("WHERE ");
        expected.Append("code = @0");
        Assert.Equal(expected.ToString(), sql.Text);
    }
}
