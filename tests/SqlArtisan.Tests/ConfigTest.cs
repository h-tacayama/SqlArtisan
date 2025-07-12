using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

[Collection("NonParallelTests")]
public class ConfigTest : IDisposable
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
        expected.Append("UPDATE test_table \"t\" ");
        expected.Append("SET \"t\".name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SetDefaultDbms_DeleteForSqlServer_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.SqlServer);

        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = @0");
        Assert.Equal(expected.ToString(), sql.Text);
    }
}
