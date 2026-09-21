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
    public void SetDefaultDbms_MySql_Select_CorrectSql()
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
    public void SetDefaultDbms_Oracle_Update_CorrectSql()
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
    public void SetDefaultDbms_SqlServer_Delete_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.SqlServer);

        // Unaliased so the plain DELETE FROM form carries the marker; the aliased
        // T-SQL form (DELETE t FROM ... AS t) is pinned by the test below.
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

    [Fact]
    public void SetDefaultDbms_SqlServer_AliasedDelete_CorrectSql()
    {
        SqlArtisanConfig.SetDefaultDbms(Dbms.SqlServer);

        SqlStatement sql =
            DeleteFrom(_t)
            .From(_t)
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE \"t\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = @0");
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SetDefaultDbms_Unknown_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SqlArtisanConfig.SetDefaultDbms(Dbms.Unknown));

        Assert.Equal(
            $"Unsupported DBMS. (Parameter 'dbms'){Environment.NewLine}Actual value was Unknown.",
            ex.Message);
    }

    [Fact]
    public void SetDefaultDbms_UndefinedValue_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SqlArtisanConfig.SetDefaultDbms((Dbms)99));

        Assert.Equal(
            $"Unsupported DBMS. (Parameter 'dbms'){Environment.NewLine}Actual value was 99.",
            ex.Message);
    }

    // Build's guard shares SetDefaultDbms's message, so it is asserted beside it:
    // the two reported different ParamNames while Build's named an internal one.
    [Fact]
    public void Build_UnknownDbms_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Select(_t.Code).From(_t).Build(Dbms.Unknown));

        Assert.Equal(
            $"Unsupported DBMS. (Parameter 'dbms'){Environment.NewLine}Actual value was Unknown.",
            ex.Message);
    }

    [Fact]
    public void Build_UndefinedDbms_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Select(_t.Code).From(_t).Build((Dbms)99));

        Assert.Equal(
            $"Unsupported DBMS. (Parameter 'dbms'){Environment.NewLine}Actual value was 99.",
            ex.Message);
    }
}
